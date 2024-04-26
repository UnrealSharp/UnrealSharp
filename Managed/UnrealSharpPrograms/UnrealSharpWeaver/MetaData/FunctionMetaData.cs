using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.TypeProcessors;

namespace UnrealSharpWeaver.MetaData;

public class FunctionMetaData : BaseMetaData
{ 
    public PropertyMetaData[] Parameters { get; set; }
    public PropertyMetaData? ReturnValue { get; set; }
    public FunctionFlags FunctionFlags { get; set; }
    public bool IsBlueprintEvent { get; set; }
    public bool IsRpc { get; set; }
    
    // Non-serialized for JSON
    public readonly MethodDefinition MethodDefinition;
    public FunctionRewriteInfo RewriteInfo;
    public FieldDefinition FunctionPointerField;
    // End non-serialized

    private const string CallInEditorName = "CallInEditor";

    public FunctionMetaData(MethodDefinition method, bool blueprintEvent = false, bool onlyCollectMetaData = false) : base(method, WeaverHelper.UFunctionAttribute)
    {
        IsBlueprintEvent = blueprintEvent; 
        MethodDefinition = method;
        bool hasOutParams = false;
        
        if (method.ReturnType.Name != WeaverHelper.VoidTypeRef.Name)
        {
            hasOutParams = true;
            try
            {
                ReturnValue = PropertyMetaData.FromTypeReference(method.ReturnType, "ReturnValue", ParameterType.ReturnValue);
            }
            catch (InvalidPropertyException e)
            {
                throw new InvalidUnrealFunctionException(method, $"'{method.ReturnType.FullName}' is invalid for unreal function return value.", e);
            }
        }

        if (BaseAttribute != null)
        {
            CustomAttributeArgument? callInEditor = WeaverHelper.FindAttributeField(BaseAttribute, CallInEditorName);
            if (callInEditor.HasValue)
            {
                TryAddMetaData(CallInEditorName, (bool) callInEditor.Value.Value);
            }
        }
        
        Parameters = new PropertyMetaData[method.Parameters.Count];
        for (int i = 0; i < method.Parameters.Count; ++i)
        {
            ParameterDefinition param = method.Parameters[i];
            
            if (param.IsOut)
            {
                hasOutParams = true;
            }

            try
            {
                bool byReference = false;
                TypeReference paramType = param.ParameterType;
                
                if (paramType.IsByReference)
                {
                    byReference = true;
                    paramType = ((ByReferenceType)paramType).ElementType;
                }

                ParameterType modifier = ParameterType.Value;
                
                if (param.IsOut)
                {
                    modifier = ParameterType.Out;
                }
                else if (byReference)
                {
                    modifier = ParameterType.Ref;
                }
                
                Parameters[i] = PropertyMetaData.FromTypeReference(paramType, param.Name, modifier);
                
            }
            catch (InvalidPropertyException e)
            {
                throw new InvalidUnrealFunctionException(method, $"'{param.ParameterType.FullName}' is invalid for unreal function parameter '{param.Name}'.", e);
            }
        }

        FunctionFlags flags = (FunctionFlags) GetFlags(method, "FunctionFlagsMapAttribute");

        if (hasOutParams)
        {
            flags |= FunctionFlags.HasOutParms;
        }
        
        if (flags.HasFlag(FunctionFlags.BlueprintNativeEvent))
        {
            IsBlueprintEvent = true;
            
            if (!method.IsVirtual && method.Body.Instructions.Count == 1 && method.Body.Instructions[0].OpCode == OpCodes.Ret)
            {
                flags ^= FunctionFlags.Native;
            }

            if (flags.HasFlag(FunctionFlags.Net))
            {
                throw new InvalidUnrealFunctionException(method, "BlueprintImplementable methods cannot be replicated!");
            }
        }

        if (method.IsPublic)
        {
            flags |= FunctionFlags.Public;
        }
        else if (method.IsFamily)
        {
            flags |= FunctionFlags.Protected;
        }
        else
        {
            flags |= FunctionFlags.Private;
        }

        if (method.IsStatic)
        {
            flags |= FunctionFlags.Static;
        }
        
        const FunctionFlags netFlags = FunctionFlags.NetServer | FunctionFlags.NetMulticast | FunctionFlags.NetClient;
        bool isRpc = (flags & netFlags) != 0;
        
        if (isRpc)
        {
            flags |= FunctionFlags.Net;
            
            if (method.ReturnType != WeaverHelper.VoidTypeRef)
            {
                throw new InvalidUnrealFunctionException(method, "RPCs can't have return values.");
            }
        }
        
        FunctionFlags = flags;
        
        if (!onlyCollectMetaData)
        {
            TypeDefinition baseType = method.GetOriginalBaseMethod().DeclaringType;
            if (baseType == method.DeclaringType)
            {
                RewriteInfo = new FunctionRewriteInfo(this);
                FunctionRewriterHelpers.PrepareFunctionForRewrite(this, method.DeclaringType);
            }
            else
            {
                FunctionRewriterHelpers.MakeImplementationMethod(this);
            }
        }
    }

    public static bool IsAsyncUFunction(MethodDefinition method)
    {
        if (!method.HasCustomAttributes)
        {
            return false;
        }

        CustomAttribute? functionAttribute = WeaverHelper.GetUFunction(method);
        if (functionAttribute == null)
        {
            return false;
        }

        if (!functionAttribute.HasConstructorArguments)
        {
            return false;
        }

        var flags = (FunctionFlags)(int)functionAttribute.ConstructorArguments[0].Value;
        if (flags != FunctionFlags.BlueprintCallable)
        {
            return false;
        }

        return method.ReturnType.FullName.StartsWith("System.Threading.Tasks.Task");
    }

    public static bool IsBlueprintEventOverride(MethodDefinition method)
    {
        if (!method.IsVirtual)
        {
            return false;
        }
        
        MethodDefinition baseMethod = method.GetOriginalBaseMethod();
        if (baseMethod != method && baseMethod.HasCustomAttributes)
        {
            return WeaverHelper.IsUFunction(baseMethod);
        }

        return false;
    }

    public static bool IsInterfaceFunction(MethodDefinition method)
    {
        foreach (var typeInterface in method.DeclaringType.Interfaces)
        {
            var interfaceType = typeInterface.InterfaceType.Resolve();
            
            if (!WeaverHelper.IsUInterface(interfaceType))
            {
                continue; 
            }

            foreach (var interfaceMethod in interfaceType.Methods)
            {
                if (interfaceMethod.Name == method.Name)
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    public void EmitFunctionPointers(ILProcessor processor, Instruction loadTypeField, Instruction setFunctionPointer)
    {
        processor.Append(loadTypeField);
        processor.Emit(OpCodes.Ldstr, Name);
        processor.Emit(OpCodes.Call, WeaverHelper.GetNativeFunctionFromClassAndNameMethod);
        processor.Append(setFunctionPointer);
    }
    
    public void EmitFunctionParamOffsets(ILProcessor processor, Instruction loadFunctionPointer)
    {
        foreach (var paramPair in RewriteInfo.FunctionParams)
        {
            FieldDefinition offsetField = paramPair.Item1;
            PropertyMetaData param = paramPair.Item2;
                
            processor.Append(loadFunctionPointer);
            processor.Emit(OpCodes.Ldstr, param.Name);
            processor.Emit(OpCodes.Call, WeaverHelper.GetPropertyOffsetFromNameMethod);
            processor.Emit(OpCodes.Stsfld, offsetField);
        }
    }
    
    public void EmitFunctionParamSize(ILProcessor processor, Instruction loadFunctionPointer)
    {
        processor.Append(loadFunctionPointer);
        processor.Emit(OpCodes.Call, WeaverHelper.GetNativeFunctionParamsSizeMethod);
        processor.Emit(OpCodes.Stsfld, RewriteInfo.FunctionParamSizeField);
    }
    
    public void EmitParamElementSize(ILProcessor processor, Instruction? loadFunctionPointer)
    {
        foreach (var pair in RewriteInfo.FunctionParamsElements)
        {
            FieldDefinition elementSizeField = pair.Item1;
            processor.Append(loadFunctionPointer);
            processor.Emit(OpCodes.Ldstr, Name);
            processor.Emit(OpCodes.Call, WeaverHelper.GetArrayElementSizeMethod);
            processor.Emit(OpCodes.Stsfld, elementSizeField);
        }
    }
    
    public bool HasParameters()
    {
        return Parameters.Length > 0 || HasReturnValue();
    }
    
    public bool HasReturnValue()
    {
        return ReturnValue != null;
    }
}
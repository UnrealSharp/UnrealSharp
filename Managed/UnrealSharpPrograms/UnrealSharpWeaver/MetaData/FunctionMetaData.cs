using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.NativeTypes;
using UnrealSharpWeaver.TypeProcessors;

namespace UnrealSharpWeaver.MetaData;

public class FunctionMetaData : BaseMetaData
{ 
    public PropertyMetaData[] Parameters { get; set; }
    public PropertyMetaData? ReturnValue { get; set; }
    public FunctionFlags FunctionFlags { get; set; }
    
    const FunctionFlags RpcFlags = FunctionFlags.NetServer | FunctionFlags.NetClient | FunctionFlags.NetMulticast;
    
    // Non-serialized for JSON
    public readonly MethodDefinition MethodDefinition;
    public FunctionRewriteInfo RewriteInfo;
    public FieldDefinition FunctionPointerField;
    public bool IsBlueprintEvent => WeaverHelper.HasAnyFlags(FunctionFlags, FunctionFlags.BlueprintNativeEvent);
    public bool HasParameters => Parameters.Length > 0 || HasReturnValue;
    public bool HasReturnValue => ReturnValue != null;
    public bool IsRpc => WeaverHelper.HasAnyFlags(FunctionFlags, RpcFlags);
    // End non-serialized

    private const string CallInEditorName = "CallInEditor";

    public FunctionMetaData(MethodDefinition method, bool onlyCollectMetaData = false) : base(method, WeaverHelper.UFunctionAttribute)
    {
        MethodDefinition = method;
        bool hasOutParams = false;
        
        if (method.ReturnType != WeaverHelper.VoidTypeRef)
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
            ParameterType modifier = ParameterType.Value;
            TypeReference paramType = param.ParameterType;
            
            if (param.IsOut)
            {
                hasOutParams = true;
                modifier = ParameterType.Out;
            }
            else if (paramType.IsByReference)
            {
                hasOutParams = true;
                modifier = ParameterType.Ref;
            }

            Parameters[i] = PropertyMetaData.FromTypeReference(paramType, param.Name, modifier);
        }

        FunctionFlags flags = (FunctionFlags) GetFlags(method, "FunctionFlagsMapAttribute");

        if (hasOutParams)
        {
            flags |= FunctionFlags.HasOutParms;
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
        
        if (WeaverHelper.HasAnyFlags(flags, RpcFlags))
        {
            flags |= FunctionFlags.Net;
            
            if (method.ReturnType != WeaverHelper.VoidTypeRef)
            {
                throw new InvalidUnrealFunctionException(method, "RPCs can't have return values.");
            }
            
            if (flags.HasFlag(FunctionFlags.BlueprintNativeEvent))
            {
                throw new InvalidUnrealFunctionException(method, "BlueprintEvents methods cannot be replicated!");
            }
        }
        
        // This represents both BlueprintNativeEvent and BlueprintImplementableEvent
        if (flags.HasFlag(FunctionFlags.BlueprintNativeEvent))
        {
            flags |= FunctionFlags.Event;
        }
        
        // Native is needed to bind the function pointer of the UFunction to our own invoke in UE.
        FunctionFlags = flags | FunctionFlags.Native;
        
        if (onlyCollectMetaData)
        {
            return;
        }
        
        RewriteFunction();
    }
    
    public void RewriteFunction()
    {
        TypeDefinition baseType = MethodDefinition.GetOriginalBaseMethod().DeclaringType;
        if (baseType == MethodDefinition.DeclaringType)
        {
            RewriteInfo = new FunctionRewriteInfo(this);
            FunctionProcessor.PrepareFunctionForRewrite(this, MethodDefinition.DeclaringType);
        }
        else
        {
            FunctionProcessor.MakeImplementationMethod(this);
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

        var flags = (FunctionFlags) (ulong) functionAttribute.ConstructorArguments[0].Value;
        return flags == FunctionFlags.BlueprintCallable && method.ReturnType.FullName.StartsWith("System.Threading.Tasks.Task");
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
        foreach (var paramRewriteInfo in RewriteInfo.FunctionParams)
        {
            FieldDefinition? offsetField = paramRewriteInfo.OffsetField;
            if (offsetField == null)
            {
                continue;
            }

            PropertyMetaData param = paramRewriteInfo.PropertyMetaData;
                
            processor.Append(loadFunctionPointer);
            processor.Emit(OpCodes.Ldstr, param.Name);
            processor.Emit(OpCodes.Call, WeaverHelper.GetPropertyOffsetFromNameMethod);
            processor.Emit(OpCodes.Stsfld, offsetField);
        }
    }
    
    public void EmitFunctionParamSize(ILProcessor processor, Instruction loadFunctionPointer)
    {
        if (RewriteInfo.FunctionParamSizeField == null)
        {
            return;
        }

        processor.Append(loadFunctionPointer);
        processor.Emit(OpCodes.Call, WeaverHelper.GetNativeFunctionParamsSizeMethod);
        processor.Emit(OpCodes.Stsfld, RewriteInfo.FunctionParamSizeField);
    }
    
    public void EmitParamNativeProperty(ILProcessor processor, Instruction? loadFunctionPointer)
    {
        foreach (var paramRewriteInfo in RewriteInfo.FunctionParams)
        {
            FieldDefinition? nativePropertyField = paramRewriteInfo.NativePropertyField;
            if (nativePropertyField == null)
            {
                continue;
            }

            processor.Append(loadFunctionPointer);
            processor.Emit(OpCodes.Ldstr, paramRewriteInfo.PropertyMetaData.Name);
            processor.Emit(OpCodes.Call, WeaverHelper.GetNativePropertyFromNameMethod);
            processor.Emit(OpCodes.Stsfld, nativePropertyField);
        }
    }
}
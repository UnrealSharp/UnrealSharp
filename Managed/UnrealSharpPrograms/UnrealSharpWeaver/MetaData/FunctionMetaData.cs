using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

using UnrealSharpWeaver.Rewriters;

namespace UnrealSharpWeaver.MetaData;

public class FunctionMetaData : BaseMetaData
{ 
    public PropertyMetaData[] Parameters { get; set; }
    public PropertyMetaData? ReturnValue { get; set; }
    public FunctionFlags FunctionFlags { get; set; }
    public bool IsBlueprintEvent { get; set; }
    public bool IsRpc { get; set; }
    public AccessProtection AccessProtection { get; set; }
    
    // Non-serialized for JSON
    public readonly MethodDefinition MethodDefinition;
    public FunctionRewriteInfo RewriteInfo;
    // End non-serialized
    
    public FunctionMetaData(MethodDefinition method, bool bOnlyCollectMetaData = false)
    {
        MethodDefinition = method;
        Name = method.Name;

        if (method.IsPublic)
        {
            AccessProtection = AccessProtection.Public;
        }
        else if (method.IsPrivate)
        {
            AccessProtection = AccessProtection.Private;
        }
        else
        {
            AccessProtection = AccessProtection.Protected;
        }

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
        
        AddMetadataAttributes(method.CustomAttributes);
        
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

        switch (AccessProtection)
        {
            case AccessProtection.Public:
                flags |= FunctionFlags.Public;
                break;
            case AccessProtection.Protected:
                flags |= FunctionFlags.Protected;
                break;
            case AccessProtection.Private:
                flags |= FunctionFlags.Private;
                break;
            default:
                throw new InvalidUnrealFunctionException(method, "Unknown access level");
        }

        CustomAttribute? ufunctionAttribute = FindAttribute(method.CustomAttributes, "UFunctionAttribute");

        if (ufunctionAttribute != null)
        {
            AddBaseAttributes(ufunctionAttribute);
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

        if (bOnlyCollectMetaData)
        {
            return;
        }
        
        RewriteInfo = new FunctionRewriteInfo(this);
        FunctionRewriterHelpers.PrepareFunctionForRewrite(this, method.DeclaringType);
    }

    public static bool IsAsyncUFunction(MethodDefinition method)
    {
        if (!method.HasCustomAttributes)
        {
            return false;
        }

        CustomAttribute? functionAttribute = FindAttribute(method.CustomAttributes, "UFunctionAttribute");
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

    public static bool IsUFunction(MethodDefinition method)
    {
        if (!method.HasCustomAttributes)
        {
            return false;
        }
        
        CustomAttribute? functionAttribute = FindAttribute(method.CustomAttributes, "UFunctionAttribute");
        return functionAttribute != null;
    }

    public static bool IsBlueprintEventOverride(MethodDefinition method)
    {
        MethodDefinition basemostMethod = method.GetOriginalBaseMethod();
        if (basemostMethod != method && basemostMethod.HasCustomAttributes)
        {
            CustomAttribute? isUnrealFunction = FindAttribute(basemostMethod.CustomAttributes, "UFunctionAttribute");
            if (isUnrealFunction != null)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsInterfaceFunction(TypeDefinition type, string methodName)
    {
        foreach (var typeInterface in type.Interfaces)
        {
            var interfaceType = typeInterface.InterfaceType.Resolve();
            if (!InterfaceMetaData.IsUInterface(interfaceType)) { continue; }

            foreach (var interfaceMethod in interfaceType.Methods)
            {
                if (interfaceMethod.Name == methodName)
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
}
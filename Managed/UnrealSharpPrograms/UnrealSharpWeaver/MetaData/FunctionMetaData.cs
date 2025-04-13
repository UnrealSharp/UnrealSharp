﻿using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.TypeProcessors;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.MetaData;

public class FunctionMetaData : BaseMetaData
{ 
    public PropertyMetaData[] Parameters { get; set; }
    public PropertyMetaData? ReturnValue { get; set; }
    public EFunctionFlags FunctionFlags { get; set; }
    
    const EFunctionFlags RpcFlags = EFunctionFlags.NetServer | EFunctionFlags.NetClient | EFunctionFlags.NetMulticast;
    
    // Non-serialized for JSON
    public readonly MethodDefinition MethodDef;
    public FunctionRewriteInfo RewriteInfo;
    public FieldDefinition FunctionPointerField;
    public bool IsBlueprintEvent => FunctionFlags.HasAnyFlags(EFunctionFlags.BlueprintNativeEvent);
    public bool HasParameters => Parameters.Length > 0 || HasReturnValue;
    public bool HasReturnValue => ReturnValue != null;
    public bool IsRpc => FunctionFlags.HasAnyFlags(RpcFlags);
    private bool _shouldBeRemoved;
    // End non-serialized

    private const string CallInEditorName = "CallInEditor";

    public FunctionMetaData(MethodDefinition method, bool onlyCollectMetaData = false) : base(method, Utilities.MethodUtilities.UFunctionAttribute)
    {
        MethodDef = method;
        bool hasOutParams = false;
        
        if (!method.ReturnsVoid())
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
            CustomAttributeArgument? callInEditor = BaseAttribute.FindAttributeField(CallInEditorName);
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

            if (param.HasConstant)
            {
                string? defaultValue = DefaultValueToString(param);
                if (defaultValue != null)
                {
                    TryAddMetaData($"CPP_Default_{param.Name}", defaultValue);
                    FunctionFlags |= EFunctionFlags.HasDefaults;
                }
            }
        }
        
        EFunctionFlags flags = (EFunctionFlags) GetFlags(method, "FunctionFlagsMapAttribute");

        if (hasOutParams)
        {
            flags |= EFunctionFlags.HasOutParms;
        }

        if (method.IsPublic)
        {
            flags |= EFunctionFlags.Public;
        }
        else if (method.IsFamily)
        {
            flags |= EFunctionFlags.Protected;
        }
        else
        {
            flags |= EFunctionFlags.Private;
        }

        if (method.IsStatic)
        {
            flags |= EFunctionFlags.Static;
        }
        
        if (flags.HasAnyFlags(RpcFlags))
        {
            flags |= EFunctionFlags.Net;
            
            if (!method.ReturnsVoid())
            {
                throw new InvalidUnrealFunctionException(method, "RPCs can't have return values.");
            }
            
            if (flags.HasFlag(EFunctionFlags.BlueprintNativeEvent))
            {
                throw new InvalidUnrealFunctionException(method, "BlueprintEvents methods cannot be replicated!");
            }
        }
        
        // This represents both BlueprintNativeEvent and BlueprintImplementableEvent
        if (flags.HasFlag(EFunctionFlags.BlueprintNativeEvent))
        {
            flags |= EFunctionFlags.Event;
        }
        
        // Native is needed to bind the function pointer of the UFunction to our own invoke in UE.
        FunctionFlags = flags | EFunctionFlags.Native;
        
        if (onlyCollectMetaData)
        {
            return;
        }
        
        RewriteFunction();
    }
    
    public void RewriteFunction()
    {
        TypeDefinition baseType = MethodDef.GetOriginalBaseMethod().DeclaringType;
        if (baseType == MethodDef.DeclaringType)
        {
            RewriteInfo = new FunctionRewriteInfo(this);
            FunctionProcessor.PrepareFunctionForRewrite(this, MethodDef.DeclaringType);
        }
        else
        {
            EFunctionFlags flags = GetFunctionFlags(MethodDef.GetOriginalBaseMethod());
            if (flags.HasAnyFlags(EFunctionFlags.BlueprintCallable) 
                && !flags.HasAnyFlags(EFunctionFlags.BlueprintNativeEvent))
            {
                return;
            }
            
            FunctionProcessor.MakeImplementationMethod(this);
            
            // We don't need the override anymore. It's copied into the Implementation method.
            // But we can't remove it here because it would mess up for child classes during weaving.
            _shouldBeRemoved = true;
        }
    }
    
    public void TryRemoveMethod()
    {
        if (!_shouldBeRemoved)
        {
            return;
        }
        
        MethodDef.DeclaringType.Methods.Remove(MethodDef);
    }
    
    public static bool IsAsyncUFunction(MethodDefinition method)
    {
        if (!method.HasCustomAttributes)
        {
            return false;
        }

        CustomAttribute? functionAttribute = method.GetUFunction();
        if (functionAttribute == null)
        {
            return false;
        }

        if (!functionAttribute.HasConstructorArguments)
        {
            return false;
        }

        var flags = (EFunctionFlags) (ulong) functionAttribute.ConstructorArguments[0].Value;
        return flags == EFunctionFlags.BlueprintCallable && method.ReturnType.FullName.StartsWith("System.Threading.Tasks.Task");
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
            return baseMethod.IsUFunction();
        }

        return false;
    }
    
    public static string? DefaultValueToString(ParameterDefinition value)
    {
        // Can be null if the value is set to = default/null
        if (value.Constant == null)
        {
            return null;
        }
        
        TypeDefinition typeDefinition = value.ParameterType.Resolve();
        if (typeDefinition.IsEnum)
        {
            return typeDefinition.Fields[(byte) value.Constant].Name;
        }
        
        // Unreal doesn't support commas in default values
        string defaultValue = value.Constant.ToString()!;
        defaultValue = defaultValue.Replace(",", ".");
        
        return defaultValue;
    }

    public static EFunctionFlags GetFunctionFlags(MethodDefinition method)
    {
        return (EFunctionFlags) GetFlags(method, "FunctionFlagsMapAttribute");
    }

    public static bool IsInterfaceFunction(MethodDefinition method)
    {
        foreach (var typeInterface in method.DeclaringType.Interfaces)
        {
            var interfaceType = typeInterface.InterfaceType.Resolve();
            
            if (!interfaceType.IsUInterface())
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
    
    public static bool IsBlueprintCallable(MethodDefinition method)
    {
        EFunctionFlags flags = GetFunctionFlags(method);
        return flags.HasAnyFlags(EFunctionFlags.BlueprintCallable);
    }
    
    public void EmitFunctionPointers(ILProcessor processor, Instruction loadTypeField, Instruction setFunctionPointer)
    {
        processor.Append(loadTypeField);
        processor.Emit(OpCodes.Ldstr, Name);
        processor.Emit(OpCodes.Call, WeaverImporter.GetNativeFunctionFromClassAndNameMethod);
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
            processor.Emit(OpCodes.Call, WeaverImporter.GetPropertyOffsetFromNameMethod);
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
        processor.Emit(OpCodes.Call, WeaverImporter.GetNativeFunctionParamsSizeMethod);
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
            processor.Emit(OpCodes.Call, WeaverImporter.GetNativePropertyFromNameMethod);
            processor.Emit(OpCodes.Stsfld, nativePropertyField);
        }
    }
}
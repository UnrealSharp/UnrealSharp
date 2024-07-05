using System;
using System.Collections.Generic;
using System.Linq;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.PropertyTranslators;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.Exporters;

public enum EFunctionProtectionMode
{
    UseUFunctionProtection,
    OverrideWithInternal,
    OverrideWithProtected,
}

public struct ExtensionMethod
{
    public UhtClass Class;
    public UhtFunction Function;
    public UhtProperty SelfParameter;
}

public enum FunctionType
{
    Normal,
    BlueprintEvent,
    ExtensionOnAnotherClass,
    InterfaceFunction,
    InternalWhitelisted
};

public enum EOverloadMode
{
    AllowOverloads,
    SuppressOverloads,
};

public enum EBlueprintVisibility
{
    Call,
    Event,
};

public struct FunctionOverload
{
    public string ParamStringAPIWithDefaults;
    public string ParamsStringCall;
    public string CSharpParamName;
    public string CppDefaultValue;
    public PropertyTranslator Translator;
    public UhtProperty Parameter;
}

public class FunctionExporter
{
    private readonly UhtFunction _function;
    private readonly string _functionName;
    private readonly List<PropertyTranslator> _parameterTranslators;
    private PropertyTranslator _returnValueTranslator;
    private readonly EOverloadMode _overloadMode;
    private readonly EFunctionProtectionMode _protectionMode;
    private readonly EBlueprintVisibility _blueprintVisibility;

    private bool _protected;
    private bool BlueprintEvent => _blueprintVisibility == EBlueprintVisibility.Event;
    private string _modifiers;
    private string _invokeFunction;
    private string _invokeFirstArgument;

    private string _customInvoke;

    private readonly string _paramStringApiWithDefaults = "";
    private readonly string _paramsStringCall = "";

    private UhtProperty? _selfParameter;
    private UhtClass? _classBeingExtended;

    private readonly List<FunctionOverload> _overloads = new();

    public FunctionExporter(ExtensionMethod extensionMethod) : this(extensionMethod.Function, EOverloadMode.AllowOverloads, EFunctionProtectionMode.UseUFunctionProtection, EBlueprintVisibility.Call)
    {
        _selfParameter = extensionMethod.SelfParameter;
        _classBeingExtended = extensionMethod.Class;
    }
    
    public FunctionExporter(UhtFunction function, EOverloadMode overloadMode, EFunctionProtectionMode protectionMode, EBlueprintVisibility blueprintVisibility)
    {
        _function = function;
        _functionName = function.SourceName;
        _overloadMode = overloadMode;
        _protectionMode = protectionMode;
        _blueprintVisibility = blueprintVisibility;

        if (function.ReturnProperty != null)
        {
            _returnValueTranslator = PropertyTranslatorManager.GetTranslator(function.ReturnProperty);
        }
        
        _parameterTranslators = new List<PropertyTranslator>();
        foreach (UhtProperty parameter in function.Properties)
        {
            PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(parameter);
            _parameterTranslators.Add(translator);
        }

        if (function.EngineType != UhtEngineType.Delegate)
        {
            if (function.SourceName == function.Outer!.EngineName)
            {
                _functionName = "Call" + _functionName;
            }
        }
        
        _protected = false;
        DetermineProtectionMode();

        if (function.HasAllFlags(EFunctionFlags.Static))
        {
            _modifiers += "static ";
            _invokeFunction = $"{ExporterCallbacks.UObjectCallbacks}.CallInvokeNativeStaticFunction";
        }
        else if (function.HasAllFlags(EFunctionFlags.Delegate))
        {
            if (function.HasParameters)
            {
                _customInvoke = "ProcessDelegate(IntPtr.Zero);";
            }
            else
            {
                _customInvoke = "ProcessDelegate(ParamsBuffer);";
            }
        }
        else
        {
            if (BlueprintEvent)
            {
                _modifiers += "virtual ";
            }
		
            if (function.IsInterfaceFunction())
            {
                _modifiers = "public ";
            }

            _invokeFunction = $"{ExporterCallbacks.UObjectCallbacks}.CallInvokeNativeFunction";
            _invokeFirstArgument = "NativeObject";
        }

        string paramString = "";
        bool hasDefaultParameters = false;

        if (_selfParameter != null)
        {
            PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(_selfParameter)!;
            string paramType = _classBeingExtended != null
                ? _classBeingExtended.EngineName
                : translator.GetManagedType(_selfParameter);
            
            paramString += $"this {paramType} self";
            _paramStringApiWithDefaults = paramString;
        }
        
        string paramsStringCallNative = "";
        foreach (UhtProperty parameter in function.Properties)
        {
            if (parameter.HasAllFlags(EPropertyFlags.ReturnParm))
            {
                continue;
            }
            
            PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(parameter)!;
            string refQualifier = "";
            if (!parameter.HasAllFlags(EPropertyFlags.ConstParm))
            {
                if (parameter.HasAllFlags(EPropertyFlags.ReferenceParm))
                {
                    refQualifier = "ref ";
                }
                else if (parameter.HasAllFlags(EPropertyFlags.OutParm))
                {
                    refQualifier = "out ";
                }
            }

            if (_selfParameter == parameter)
            {
                if (string.IsNullOrEmpty(_paramsStringCall))
                {
                    _paramsStringCall += _selfParameter.SourceName;
                }
                else
                {
                    paramString = $"{_selfParameter.SourceName},  " + _paramsStringCall.Substring(0, _paramsStringCall.Length - 2);
                }
                paramsStringCallNative += _selfParameter.SourceName;
            }
            else
            {
                string cppDefaultValue = translator.GetCppDefaultValue(function, parameter);

                if (cppDefaultValue == "()" && parameter is UhtStructProperty structProperty)
                {
                    _paramsStringCall += $"new {structProperty.ScriptStruct.EngineName}()";
                }
                else
                {
                    _paramsStringCall += $"{refQualifier}{parameter.SourceName}";
                }

                paramsStringCallNative += $"{refQualifier}{parameter.SourceName}";
                paramString += $"{refQualifier}{translator.GetManagedType(parameter)} {parameter.SourceName}";

                if ((hasDefaultParameters || cppDefaultValue.Length > 0) && _overloadMode == EOverloadMode.AllowOverloads)
                {
                    hasDefaultParameters = true;
                    string csharpDefaultValue = "";
                    
                    if (cppDefaultValue.Length > 0)
                    {
                        csharpDefaultValue = translator.GetNullValue(parameter);
                    }
                    else if (translator.ExportDefaultParameter)
                    {
                        csharpDefaultValue = translator.ConvertCPPDefaultValue(cppDefaultValue, function, parameter);
                    }

                    if (string.IsNullOrEmpty(csharpDefaultValue))
                    {
                        string defaultValue = $" = {csharpDefaultValue}";
                        _paramStringApiWithDefaults += $"{refQualifier}{translator.GetManagedType(parameter)} {parameter.SourceName}{defaultValue}";
                    }
                    else
                    {
                        if (_paramStringApiWithDefaults.Length > 0)
                        {
                            _paramStringApiWithDefaults = _paramStringApiWithDefaults.Substring(0, _paramStringApiWithDefaults.Length - 2);
                        }
                        
                        FunctionOverload overload = new FunctionOverload
                        {
                            ParamStringAPIWithDefaults = _paramStringApiWithDefaults,
                            ParamsStringCall = _paramsStringCall,
                            CSharpParamName = parameter.SourceName,
                            CppDefaultValue = cppDefaultValue,
                            Translator = translator,
                            Parameter = parameter,
                        };
                        
                        _overloads.Add(overload);
                        
                        _paramStringApiWithDefaults = paramString;
                    }
                }
                else
                {
                    _paramStringApiWithDefaults = paramString;
                }
                
                paramString += ", ";
                _paramStringApiWithDefaults += ", ";
            }
            
            _paramsStringCall += ", ";
            paramsStringCallNative += ", ";
        }

        if (_selfParameter != null)
        {
            _paramsStringCall = paramsStringCallNative;
        }
        
        // remove last comma
        if (_paramStringApiWithDefaults.Length > 0)
        {
            _paramStringApiWithDefaults = _paramStringApiWithDefaults.Substring(0, _paramStringApiWithDefaults.Length - 2);
        }  
        
        if (_paramsStringCall.Length > 0)
        {
            _paramsStringCall = paramString.Substring(0, paramString.Length - 2);
        }
    }

    public static void ExportFunction(GeneratorStringBuilder builder, UhtFunction function, FunctionType functionType)
    {
        EFunctionProtectionMode protectionMode = EFunctionProtectionMode.UseUFunctionProtection;
        EOverloadMode overloadMode = EOverloadMode.AllowOverloads;
        EBlueprintVisibility blueprintVisibility = EBlueprintVisibility.Call;

        if (functionType == FunctionType.ExtensionOnAnotherClass)
        {
            protectionMode = EFunctionProtectionMode.OverrideWithInternal;
            overloadMode = EOverloadMode.SuppressOverloads;
        }
        else if (functionType == FunctionType.BlueprintEvent)
        {
            overloadMode = EOverloadMode.SuppressOverloads;
            blueprintVisibility = EBlueprintVisibility.Event;
        }
        else if (functionType == FunctionType.InternalWhitelisted)
        {
            protectionMode = EFunctionProtectionMode.OverrideWithProtected;
        }
        
        builder.TryAddWithEditor(function);
        
        FunctionExporter exporter = new FunctionExporter(function, overloadMode, protectionMode, blueprintVisibility);
        exporter.ExportFunctionVariables(builder);
        exporter.ExportOverloads(builder);
        exporter.ExportFunction(builder);
        
        builder.TryEndWithEditor(function);
    }
    
    public static void ExportOverridableFunction(GeneratorStringBuilder builder, UhtFunction function)
    {
        builder.TryAddWithEditor(function);
        
        string ParamsStringAPI = "";
        string ParamsCallString = "";
        string methodName = function.SourceName;
        
        builder.TryAddWithEditor(function);

        foreach (UhtProperty parameter in function.Properties)
        {
            if (parameter.HasAllFlags(EPropertyFlags.ReturnParm))
            {
                continue;
            }
            
            string paramName = parameter.SourceName;
            string paramType = PropertyTranslatorManager.GetTranslator(parameter)!.GetManagedType(parameter);
            
            string refQualifier = "";
            if (!parameter.HasAllFlags(EPropertyFlags.ConstParm))
            {
                if (parameter.HasAllFlags(EPropertyFlags.ReferenceParm))
                {
                    refQualifier = "ref ";
                }
                else if (parameter.HasAllFlags(EPropertyFlags.OutParm))
                {
                    refQualifier = "out ";
                }
            }
            
            ParamsStringAPI += $"{refQualifier}{paramType} {paramName}, ";
            ParamsStringAPI += ", ";
            ParamsCallString += $"{refQualifier}{paramName}, ";
        }
        
        if (ParamsStringAPI.Length > 0)
        {
            ParamsStringAPI = ParamsStringAPI.Substring(0, ParamsStringAPI.Length - 2);
        }
        if (ParamsCallString.Length > 0)
        {
            ParamsCallString = ParamsCallString.Substring(0, ParamsCallString.Length - 2);
        }
        
        ExportFunction(builder, function, FunctionType.BlueprintEvent);
        
        string returnType = function.ReturnProperty != null
            ? PropertyTranslatorManager.GetTranslator(function.ReturnProperty)!.GetManagedType(function.ReturnProperty)
            : "void";
        
        builder.AppendLine("// Hide implementation function from Intellisense");
        builder.AppendLine("[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]");
        builder.AppendLine($"protected virtual {returnType} {methodName}_Implementation({ParamsStringAPI})");
        builder.OpenBrace();
        
        foreach (UhtProperty parameter in function.Properties)
        {
            if (parameter.HasAllFlags(EPropertyFlags.OutParm) 
                && !parameter.HasAnyFlags(EPropertyFlags.ReturnParm | EPropertyFlags.ConstParm | EPropertyFlags.ReferenceParm))
            {
                PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(parameter)!;
                string paramName = parameter.SourceName;
                string nullValue = translator.GetNullValue(parameter);
                builder.AppendLine($"{paramName} = {nullValue};");
            }
        }

        if (function.ReturnProperty != null)
        {
            PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(function.ReturnProperty)!;
            string nullValue = translator.GetNullValue(function.ReturnProperty);
            builder.AppendLine($"return {nullValue};");
        }
        
        builder.CloseBrace();

        string returnAssignment = "";
        foreach (UhtProperty parameter in function.Properties)
        {
            PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(parameter)!;
            string paramType = translator.GetManagedType(parameter);

            if (parameter.HasAllFlags(EPropertyFlags.ReturnParm))
            {
                returnAssignment = $"{paramType} returnValue = ";
            }
            else if (!parameter.HasAnyFlags(EPropertyFlags.ConstParm) && parameter.HasAnyFlags(EPropertyFlags.OutParm))
            {
                builder.AppendLine($"{paramType} {parameter.SourceName} = default;");
            }
            else
            {
                returnAssignment = $"{paramType} {parameter.SourceName} = ";
                string offsetName = $"{methodName}_{parameter.SourceName}_Offset";
                translator.ExportFromNative(builder, parameter, parameter.SourceName, returnAssignment, "buffer", offsetName, false, false);
            }
        }
        
        builder.AppendLine($"{returnAssignment}{methodName}_Implementation({ParamsCallString});");

        if (function.ReturnProperty != null)
        {
            PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(function.ReturnProperty)!;
            translator.ExportToNative(builder, function.ReturnProperty, function.ReturnProperty.SourceName, "returnBuffer", "0", "returnValue");
        }
        
        foreach (UhtProperty parameter in function.Properties)
        {
            if (!parameter.HasAllFlags(EPropertyFlags.ReturnParm | EPropertyFlags.ConstParm) && parameter.HasAllFlags(EPropertyFlags.OutParm))
            {
                PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(parameter)!;
                translator.ExportToNative(builder, parameter, parameter.SourceName, "buffer", $"{methodName}_{parameter.SourceName}_Offset", parameter.SourceName);
            }
        }
        
        builder.EndUnsafeBlock();
        builder.CloseBrace();
        
        builder.TryEndWithEditor(function);
        
        builder.AppendLine();
    }

    public static void ExportDelegateFunction(GeneratorStringBuilder builder, UhtFunction function)
    {
        FunctionExporter exporter = new FunctionExporter(function, EOverloadMode.SuppressOverloads,
            EFunctionProtectionMode.OverrideWithProtected, EBlueprintVisibility.Call);
        
        PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(function.ReturnProperty)!;
        string returnType = translator.GetManagedType(function.ReturnProperty);
        
        builder.AppendLine($"public delegate {returnType} Signature({exporter._paramStringApiWithDefaults});");
        builder.AppendLine();
        exporter.ExportFunctionVariables(builder);
        builder.AppendLine();
        builder.AppendLine($"protected {returnType} Invoker({exporter._paramStringApiWithDefaults})");
        builder.OpenBrace();
        builder.BeginUnsafeBlock();
        exporter.ExportInvoke(builder);
        builder.EndUnsafeBlock();
        builder.CloseBrace();
    }
    
    public static void ExportInterfaceFunction(GeneratorStringBuilder builder, UhtFunction function)
    {
        builder.TryAddWithEditor(function);
        
        FunctionExporter exporter = new FunctionExporter(function, EOverloadMode.AllowOverloads, EFunctionProtectionMode.UseUFunctionProtection, EBlueprintVisibility.Call);
        exporter.ExportSignature(builder, "public ");
        builder.AppendLine(";");
    }
    
    public static void ExportExtensionMethod(GeneratorStringBuilder builder, ExtensionMethod extensionMethod)
    {
        builder.TryAddWithEditor(extensionMethod.Function);
        FunctionExporter exporter = new FunctionExporter(extensionMethod);
        exporter.ExportFunction(builder);
        exporter.ExportOverloads(builder);
        builder.TryEndWithEditor(extensionMethod.Function);
    }

    void ExportFunctionVariables(GeneratorStringBuilder builder)
    {
        builder.AppendLine($"// {_function.SourceName}");
        
        string staticDeclaration = BlueprintEvent ? "" : "static ";
        builder.AppendLine($"{staticDeclaration}IntPtr {_function.SourceName}_NativeFunction;");
        
        if (_function.HasParameters)
        {
            builder.AppendLine($"static int {_function.SourceName}_ParamsSize;");
        }
        
        foreach (UhtProperty parameter in _function.Properties)
        {
            PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(parameter)!;
            translator.ExportParameterVariables(builder, _function, _function.SourceName, parameter, parameter.SourceName);
        }
    }

    void ExportOverloads(GeneratorStringBuilder builder)
    {
        foreach (FunctionOverload overload in _overloads)
        {
            builder.AppendLine();
            ExportDeprecation(builder);

            string returnType = "void";
            string returnStatement = "";
            if (_function.ReturnProperty != null)
            {
                returnType = _returnValueTranslator.GetManagedType(_function.ReturnProperty);
                returnStatement = "return ";
            }
            
            builder.AppendLine($"{_modifiers}{returnType} {_function.SourceName}({overload.ParamStringAPIWithDefaults})");
            builder.OpenBrace();
            overload.Translator.ExportCppDefaultParameterAsLocalVariable(builder, overload.CSharpParamName, overload.CppDefaultValue, _function, overload.Parameter);
            builder.AppendLine($"{returnStatement}{_function.SourceName}({overload.ParamsStringCall});");
            builder.CloseBrace();
        }
    }

    void ExportFunction(GeneratorStringBuilder builder)
    {
        builder.AppendLine();
        ExportDeprecation(builder);

        if (BlueprintEvent)
        {
            builder.AppendLine("[UFunction(FunctionFlags.BlueprintEvent)]");
        }
        
        ExportSignature(builder, _modifiers);
        builder.OpenBrace();
        builder.BeginUnsafeBlock();
        
        ExportInvoke(builder);
        
        builder.CloseBrace();
        builder.EndUnsafeBlock();
        builder.AppendLine();
    }

    void ExportInvoke(GeneratorStringBuilder builder)
    {
        string nativeFunctionvarName = $"{_function.SourceName}_NativeFunction";

        if (BlueprintEvent)
        {
            builder.AppendLine($"if ({nativeFunctionvarName} == IntPtr.Zero)");
            builder.OpenBrace();
            builder.AppendLine($"{nativeFunctionvarName} = {ExporterCallbacks.UClassCallbacks}.CallGetNativeFunctionFromInstanceAndName(NativeClassPtr, \"{_function.SourceName}\");");
            builder.CloseBrace();
        }

        if (!_function.HasParameters)
        {
            if (string.IsNullOrEmpty(_customInvoke))
            {
                builder.AppendLine($"{_invokeFunction}({_invokeFirstArgument}, {nativeFunctionvarName}, IntPtr.Zero);");
            }
            else
            {
                builder.AppendLine(_customInvoke);
            }
        }
        else
        {
            builder.AppendLine($"byte* ParamsBufferAllocation = stackalloc byte[{_functionName}_ParamsSize];");
            builder.AppendLine("nint ParamsBuffer = (nint) ParamsBufferAllocation;");
            builder.AppendLine($"{ExporterCallbacks.UStructCallbacks}.CallInitializeStruct({nativeFunctionvarName}, ParamsBuffer);");
            
            foreach (UhtProperty parameter in _function.Properties)
            {
                if (parameter.HasAllFlags(EPropertyFlags.ReturnParm))
                {
                    continue;
                }
                
                string propertyName = parameter.SourceName;
                
                if (parameter.HasAllFlags(EPropertyFlags.ReferenceParm) || !parameter.HasAllFlags(EPropertyFlags.OutParm))
                {
                    PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(parameter)!;
                    string offsetName = $"{_functionName}_{propertyName}_Offset";
                    translator.ExportToNative(builder, parameter, propertyName, "ParamsBuffer", offsetName, propertyName);
                }
            }
            
            builder.AppendLine();

            if (string.IsNullOrEmpty(_customInvoke))
            {
                builder.AppendLine($"{_invokeFunction}({_invokeFirstArgument}, {nativeFunctionvarName}, ParamsBuffer);");
            }
            else
            {
                builder.AppendLine(_customInvoke);
            }

            if (_function.ReturnProperty != null || _function.HasOutParams())
            {
                builder.AppendLine();

                foreach (UhtProperty parameter in _function.Properties)
                {
                    PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(parameter)!;
                    if (parameter.HasAllFlags(EPropertyFlags.ReturnParm) || 
                        (!parameter.HasAllFlags(EPropertyFlags.ConstParm) && parameter.HasAllFlags(EPropertyFlags.OutParm)))
                    {
                        string marshalDestination;
                        if (parameter.HasAllFlags(EPropertyFlags.ReturnParm))
                        {
                            builder.AppendLine($"{_returnValueTranslator.GetManagedType(parameter)} returnValue;");
                            marshalDestination = "returnValue";
                        }
                        else
                        {
                            marshalDestination = parameter.SourceName;
                        }
                        
                        translator.ExportFromNative(builder, 
                            parameter, 
                            parameter.SourceName, 
                            $"{marshalDestination} =", 
                            "ParamsBuffer", 
                            $"{_functionName}_{parameter.SourceName}_Offset", 
                            true, 
                            parameter.HasAllFlags(EPropertyFlags.ReferenceParm) && !parameter.HasAllFlags(EPropertyFlags.OutParm));
                    }
                }
            }
            
            builder.AppendLine();
            
            foreach (UhtProperty parameter in _function.Properties)
            {
                if (!parameter.HasAnyFlags(EPropertyFlags.ReturnParm | EPropertyFlags.OutParm))
                {
                    PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(parameter)!;
                    translator.ExportCleanupMarshallingBuffer(builder, parameter, parameter.SourceName);
                }
            }
            
            if (_function.ReturnProperty != null)
            {
                builder.AppendLine("return returnValue;");
            }
        }
    }

    void ExportSignature(GeneratorStringBuilder builder, string protection)
    {
        string returnType = _function.ReturnProperty != null
            ? _returnValueTranslator.GetManagedType(_function.ReturnProperty)
            : "void";
        builder.AppendLine($"{protection}{returnType} {_functionName}({_paramStringApiWithDefaults})");
    }

    void ExportDeprecation(GeneratorStringBuilder builder)
    {
        if (_function.HasMetaData("DeprecatedFunction"))
        {
            string deprecationMessage = _function.GetMetaData("DeprecationMessage");
            if (deprecationMessage.Length == 0)
            {
                deprecationMessage = "This function is deprecated.";
            }
            builder.AppendLine($"[Obsolete(\"{_function.SourceName} is deprecated: {deprecationMessage}\")]");
        }
    }

    void DetermineProtectionMode()
    {
        switch (_protectionMode)
        {
            case EFunctionProtectionMode.UseUFunctionProtection:
                if (_function.HasAllFlags(EFunctionFlags.Public))
                {
                    _modifiers = "public ";
                }
                else if (_function.HasAllFlags(EFunctionFlags.Protected) || _function.HasMetaData("BlueprintProtected"))
                {
                    _modifiers = "protected ";
                    _protected = true;
                }
                else
                {
                    _modifiers = "public ";
                }
                break;
            case EFunctionProtectionMode.OverrideWithInternal:
                _modifiers = "internal ";
                break;
            case EFunctionProtectionMode.OverrideWithProtected:
                _modifiers = "protected ";
                break;
        }
    }
}
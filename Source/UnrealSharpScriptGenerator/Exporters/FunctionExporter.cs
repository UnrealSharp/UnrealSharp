using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.PropertyTranslators;
using UnrealSharpScriptGenerator.Tooltip;
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
    InternalWhitelisted
};

public enum OverloadMode
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
    public string ParamStringApiWithDefaults;
    public string ParamsStringCall;
    public string CSharpParamName;
    public string CppDefaultValue;
    public PropertyTranslator Translator;
    public UhtProperty Parameter;
}

public class FunctionExporter
{
    private static readonly Dictionary<UhtPackage, List<ExtensionMethod>> ExtensionMethods = new();
    
    private readonly UhtFunction _function;
    private string _functionName = null!;
    private List<PropertyTranslator> _parameterTranslators = null!;
    private PropertyTranslator? ReturnValueTranslator => _function.ReturnProperty != null ? _parameterTranslators.Last() : null;
    private OverloadMode _overloadMode = OverloadMode.AllowOverloads;
    private EFunctionProtectionMode _protectionMode = EFunctionProtectionMode.UseUFunctionProtection;
    private EBlueprintVisibility _blueprintVisibility = EBlueprintVisibility.Call;
    
    private bool BlueprintEvent => _blueprintVisibility == EBlueprintVisibility.Event;
    private string _modifiers = "";
    private string _invokeFunction = "";
    private string _invokeFirstArgument = "";

    private string _customInvoke = "";

    private string _paramStringApiWithDefaults = "";
    private string _paramsStringCall = "";

    private readonly UhtProperty? _selfParameter;
    private readonly UhtClass? _classBeingExtended;

    private readonly List<FunctionOverload> _overloads = new();

    public FunctionExporter(ExtensionMethod extensionMethod)
    {
        _selfParameter = extensionMethod.SelfParameter;
        _function = extensionMethod.Function;
        _classBeingExtended = extensionMethod.Class;
        
        Initialize(OverloadMode.AllowOverloads, EFunctionProtectionMode.UseUFunctionProtection, EBlueprintVisibility.Call);
    }

    private FunctionExporter(UhtFunction function, OverloadMode overloadMode, EFunctionProtectionMode protectionMode, EBlueprintVisibility blueprintVisibility) 
    {
        _function = function;
        Initialize(overloadMode, protectionMode, blueprintVisibility);
    }

    private void Initialize(OverloadMode overloadMode, EFunctionProtectionMode protectionMode, EBlueprintVisibility blueprintVisibility)
    {
        _functionName = _function.GetFunctionName();
        _overloadMode = overloadMode;
        _protectionMode = protectionMode;
        _blueprintVisibility = blueprintVisibility;
        
        _parameterTranslators = new List<PropertyTranslator>(_function.Children.Count);
        foreach (UhtProperty parameter in _function.Properties)
        {
            PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(parameter)!;
            _parameterTranslators.Add(translator);
        }
        
        DetermineProtectionMode();

        if (_function.HasAllFlags(EFunctionFlags.Static))
        {
            _modifiers += "static ";
            _invokeFunction = $"{ExporterCallbacks.UObjectCallbacks}.CallInvokeNativeStaticFunction";
            _invokeFirstArgument = "NativeClassPtr";
        }
        else if (_function.HasAllFlags(EFunctionFlags.Delegate))
        {
            if (_function.HasParametersOrReturnValue())
            {
                _customInvoke = "ProcessDelegate(ParamsBuffer);";
            }
            else
            {
                _customInvoke = "ProcessDelegate(IntPtr.Zero);";
            }
        }
        else
        {
            if (BlueprintEvent)
            {
                _modifiers += "virtual ";
            }
		
            if (_function.IsInterfaceFunction())
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
                ? _classBeingExtended.GetStructName()
                : translator.GetManagedType(_selfParameter);
            
            paramString = $"this {paramType} {_selfParameter.GetParameterName()}, ";
            _paramStringApiWithDefaults = paramString;
        }
        
        string paramsStringCallNative = "";
        for (int i = 0; i < _function.Children.Count; i++)
        {
            UhtProperty parameter = (UhtProperty) _function.Children[i];
            if (parameter.HasAllFlags(EPropertyFlags.ReturnParm))
            {
                continue;
            }
            
            PropertyTranslator translator = _parameterTranslators[i];
            
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

            string parameterName = parameter.GetParameterName();

            if (_selfParameter == parameter)
            {
                if (string.IsNullOrEmpty(_paramsStringCall))
                {
                    _paramsStringCall += parameterName;
                }
                else
                {
                    _paramsStringCall = $"{parameterName},  " + _paramsStringCall.Substring(0, _paramsStringCall.Length - 2);
                }
                paramsStringCallNative += parameterName;
            }
            else
            {
                string cppDefaultValue = translator.GetCppDefaultValue(_function, parameter);
                
                if (cppDefaultValue == "()" && parameter is UhtStructProperty structProperty)
                {
                    _paramsStringCall += $"new {structProperty.ScriptStruct.GetFullManagedName()}()";
                }
                else
                {
                    _paramsStringCall += $"{refQualifier}{parameterName}";
                }

                paramsStringCallNative += $"{refQualifier}{parameterName}";
                paramString += $"{refQualifier}{translator.GetManagedType(parameter)} {parameterName}";

                if ((hasDefaultParameters || cppDefaultValue.Length > 0) && _overloadMode == OverloadMode.AllowOverloads)
                {
                    hasDefaultParameters = true;
                    string csharpDefaultValue = "";
                    
                    if (cppDefaultValue.Length == 0 || cppDefaultValue == "None")
                    {
                        csharpDefaultValue = translator.GetNullValue(parameter);
                    }
                    else if (translator.ExportDefaultParameter)
                    {
                        csharpDefaultValue = translator.ConvertCPPDefaultValue(cppDefaultValue, _function, parameter);
                    }
                    
                    if (!string.IsNullOrEmpty(csharpDefaultValue))
                    {
                        string defaultValue = $" = {csharpDefaultValue}";
                        _paramStringApiWithDefaults += $"{refQualifier}{translator.GetManagedType(parameter)} {parameterName}{defaultValue}";
                    }
                    else
                    {
                        if (_paramStringApiWithDefaults.Length > 0)
                        {
                            _paramStringApiWithDefaults = _paramStringApiWithDefaults.Substring(0, _paramStringApiWithDefaults.Length - 2);
                        }
                        
                        FunctionOverload overload = new FunctionOverload
                        {
                            ParamStringApiWithDefaults = _paramStringApiWithDefaults,
                            ParamsStringCall = _paramsStringCall,
                            CSharpParamName = parameterName,
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
            _paramsStringCall = _paramsStringCall.Substring(0, _paramsStringCall.Length - 2);
        }
    }
    
    public static void TryAddExtensionMethod(UhtFunction function)
    {
        if (!function.HasMetadata("ExtensionMethod") || function.Children.Count == 0)
        {
            return;
        }

        UhtPackage package = function.Outer!.Package;
        if (!ExtensionMethods.TryGetValue(package, out var extensionMethods))
        {
            extensionMethods = new List<ExtensionMethod>();
            ExtensionMethods.Add(package, extensionMethods);
        }
        
        UhtProperty firstParameter = (function.Children[0] as UhtProperty)!;
        ExtensionMethod newExtensionMethod = new ExtensionMethod
        {
            Function = function,
            SelfParameter = firstParameter,
        };
        
        if (firstParameter is UhtObjectPropertyBase objectSelfProperty)
        {
            newExtensionMethod.Class = objectSelfProperty.MetaClass!;
        }
        
        extensionMethods.Add(newExtensionMethod);
    }
    
    public static void StartExportingExtensionMethods(List<Task> tasks)
    {
        foreach (KeyValuePair<UhtPackage, List<ExtensionMethod>> extensionInfo in ExtensionMethods)
        {
            tasks.Add(Program.Factory.CreateTask(_ =>
            {
                ExtensionsClassExporter.ExportExtensionsClass(extensionInfo.Key, extensionInfo.Value); 
            })!);
        }
    }
    public static void ExportFunction(GeneratorStringBuilder builder, UhtFunction function, FunctionType functionType)
    {
        EFunctionProtectionMode protectionMode = EFunctionProtectionMode.UseUFunctionProtection;
        OverloadMode overloadMode = OverloadMode.AllowOverloads;
        EBlueprintVisibility blueprintVisibility = EBlueprintVisibility.Call;

        if (functionType == FunctionType.ExtensionOnAnotherClass)
        {
            protectionMode = EFunctionProtectionMode.OverrideWithInternal;
            overloadMode = OverloadMode.SuppressOverloads;
        }
        else if (functionType == FunctionType.BlueprintEvent)
        {
            overloadMode = OverloadMode.SuppressOverloads;
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
        
        string paramsStringApi = "";
        string paramsCallString = "";
        string methodName = function.GetFunctionName();

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
            
            paramsStringApi += $"{refQualifier}{paramType} {paramName}, ";
            paramsCallString += $"{refQualifier}{paramName}, ";
        }
        
        if (paramsStringApi.Length > 0)
        {
            paramsStringApi = paramsStringApi.Substring(0, paramsStringApi.Length - 2);
        }
        if (paramsCallString.Length > 0)
        {
            paramsCallString = paramsCallString.Substring(0, paramsCallString.Length - 2);
        }
        
        ExportFunction(builder, function, FunctionType.BlueprintEvent);
        
        string returnType = function.ReturnProperty != null
            ? PropertyTranslatorManager.GetTranslator(function.ReturnProperty)!.GetManagedType(function.ReturnProperty)
            : "void";
        
        builder.AppendLine("// Hide implementation function from Intellisense");
        builder.AppendLine("[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]");
        builder.AppendLine($"protected virtual {returnType} {methodName}_Implementation({paramsStringApi})");
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
        
        builder.AppendLine($"void Invoke_{function.EngineName}(IntPtr buffer, IntPtr returnBuffer)");
        builder.OpenBrace();
        builder.BeginUnsafeBlock();
        
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
                string assignmentOrReturn = $"{paramType} {parameter.SourceName} = ";
                string offsetName = $"{function.SourceName}_{parameter.SourceName}_Offset";
                translator.ExportFromNative(builder, parameter, parameter.SourceName, assignmentOrReturn, "buffer", offsetName, false, false);
            }
        }
        
        builder.AppendLine($"{returnAssignment}{methodName}_Implementation({paramsCallString});");

        if (function.ReturnProperty != null)
        {
            PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(function.ReturnProperty)!;
            translator.ExportToNative(builder, function.ReturnProperty, function.ReturnProperty.SourceName, "returnBuffer", "0", "returnValue");
        }
        
        foreach (UhtProperty parameter in function.Properties)
        {
            if (!parameter.HasAnyFlags(EPropertyFlags.ReturnParm | EPropertyFlags.ConstParm) && parameter.HasAnyFlags(EPropertyFlags.OutParm))
            {
                PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(parameter)!;
                string offsetName = $"{function.SourceName}_{parameter.SourceName}_Offset";
                translator.ExportToNative(builder, parameter, parameter.SourceName, "buffer", offsetName, parameter.SourceName);
            }
        }
        
        builder.EndUnsafeBlock();
        builder.CloseBrace();
        
        builder.TryEndWithEditor(function);
        
        builder.AppendLine();
    }

    public static void ExportDelegateFunction(GeneratorStringBuilder builder, UhtFunction function)
    {
        FunctionExporter exporter = new FunctionExporter(function, OverloadMode.SuppressOverloads,
            EFunctionProtectionMode.OverrideWithProtected, EBlueprintVisibility.Call);
        
        builder.AppendLine($"public delegate void Signature({exporter._paramStringApiWithDefaults});");
        builder.AppendLine();
        exporter.ExportFunctionVariables(builder);
        builder.AppendLine();
        builder.AppendLine($"protected void Invoker({exporter._paramStringApiWithDefaults})");
        builder.OpenBrace();
        builder.BeginUnsafeBlock();
        exporter.ExportInvoke(builder);
        builder.EndUnsafeBlock();
        builder.CloseBrace();
    }
    
    public static void ExportInterfaceFunction(GeneratorStringBuilder builder, UhtFunction function)
    {
        builder.TryAddWithEditor(function);

        FunctionExporter exporter = new FunctionExporter(function, OverloadMode.AllowOverloads, EFunctionProtectionMode.UseUFunctionProtection, EBlueprintVisibility.Call);
        exporter.ExportSignature(builder, "public ");
        builder.Append(";");
    }
    
    public void ForEachParameter(Action<PropertyTranslator, UhtProperty> action)
    {
        for (int i = 0; i < _function.Children.Count; i++)
        {
            UhtProperty parameter = (UhtProperty) _function.Children[i];
            PropertyTranslator translator = _parameterTranslators[i];
            action(translator, parameter);
        }
    }
    
    public void ExportExtensionMethod(GeneratorStringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendTooltip(_function);
        ExportDeprecation(builder);

        string returnManagedType = "void";
        if (ReturnValueTranslator != null)
        {
            returnManagedType = ReturnValueTranslator.GetManagedType(_function.ReturnProperty!);
        }
        
        builder.AppendLine($"{_modifiers}{returnManagedType} {_functionName}({_paramStringApiWithDefaults})");
        builder.OpenBrace();
        string returnStatement = _function.ReturnProperty != null ? "return " : "";
        UhtClass functionOwner = (UhtClass) _function.Outer!;

        string fullClassName = functionOwner.GetFullManagedName();
        builder.AppendLine($"{returnStatement}{fullClassName}.{_functionName}({_paramsStringCall});");
        builder.CloseBrace();
    }

    void ExportFunctionVariables(GeneratorStringBuilder builder)
    {
        builder.AppendLine($"// {_function.SourceName}");
        
        string staticDeclaration = BlueprintEvent ? "" : "static ";
        builder.AppendLine($"{staticDeclaration}IntPtr {_function.SourceName}_NativeFunction;");
        
        if (_function.HasParametersOrReturnValue())
        {
            builder.AppendLine($"static int {_function.SourceName}_ParamsSize;");
        }
        
        ForEachParameter((translator, parameter) =>
        {
            translator.ExportParameterVariables(builder, _function, _function.SourceName, parameter, parameter.SourceName);
        });
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
                returnType = ReturnValueTranslator!.GetManagedType(_function.ReturnProperty);
                returnStatement = "return ";
            }
            
            builder.AppendLine($"{_modifiers}{returnType} {_functionName}({overload.ParamStringApiWithDefaults})");
            builder.OpenBrace();
            overload.Translator.ExportCppDefaultParameterAsLocalVariable(builder, overload.CSharpParamName, overload.CppDefaultValue, _function, overload.Parameter);
            builder.AppendLine($"{returnStatement}{_functionName}({overload.ParamsStringCall});");
            builder.CloseBrace();
        }
    }
    
    void ExportFunction(GeneratorStringBuilder builder)
    {
        builder.AppendLine();
        ExportDeprecation(builder);
        
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
        string nativeFunctionIntPtr = $"{_function.SourceName}_NativeFunction";

        if (BlueprintEvent)
        {
            builder.AppendLine($"if ({nativeFunctionIntPtr} == IntPtr.Zero)");
            builder.OpenBrace();
            builder.AppendLine($"{nativeFunctionIntPtr} = {ExporterCallbacks.UClassCallbacks}.CallGetNativeFunctionFromInstanceAndName(NativeObject, \"{_function.EngineName}\");");
            builder.CloseBrace();
        }
        
        if (!_function.HasParametersOrReturnValue())
        {
            if (string.IsNullOrEmpty(_customInvoke))
            {
                builder.AppendLine($"{_invokeFunction}({_invokeFirstArgument}, {nativeFunctionIntPtr}, IntPtr.Zero);");
            }
            else
            {
                builder.AppendLine(_customInvoke);
            }
        }
        else
        {
            builder.AppendLine($"byte* ParamsBufferAllocation = stackalloc byte[{_function.SourceName}_ParamsSize];");
            builder.AppendLine("nint ParamsBuffer = (nint) ParamsBufferAllocation;");
            builder.AppendLine($"{ExporterCallbacks.UStructCallbacks}.CallInitializeStruct({nativeFunctionIntPtr}, ParamsBuffer);");
            
            ForEachParameter((translator, parameter) =>
            {
                if (parameter.HasAllFlags(EPropertyFlags.ReturnParm))
                {
                    return;
                }
                
                string propertyName = parameter.GetParameterName();
                
                if (parameter.HasAllFlags(EPropertyFlags.ReferenceParm) || !parameter.HasAllFlags(EPropertyFlags.OutParm))
                {
                    string offsetName = $"{_function.SourceName}_{parameter.SourceName}_Offset";
                    translator.ExportToNative(builder, parameter, parameter.SourceName, "ParamsBuffer", offsetName, propertyName);
                }
            });
            
            builder.AppendLine();

            if (string.IsNullOrEmpty(_customInvoke))
            {
                builder.AppendLine($"{_invokeFunction}({_invokeFirstArgument}, {nativeFunctionIntPtr}, ParamsBuffer);");
            }
            else
            {
                builder.AppendLine(_customInvoke);
            }

            if (_function.ReturnProperty != null || _function.HasOutParams())
            {
                builder.AppendLine();

                ForEachParameter((translator, parameter) =>
                {
                    if (!parameter.HasAllFlags(EPropertyFlags.ReturnParm) &&
                        (parameter.HasAllFlags(EPropertyFlags.ConstParm) ||
                         !parameter.HasAllFlags(EPropertyFlags.OutParm)))
                    {
                        return;
                    }

                    string marshalDestination;
                    if (parameter.HasAllFlags(EPropertyFlags.ReturnParm))
                    {
                        builder.AppendLine($"{ReturnValueTranslator!.GetManagedType(parameter)} returnValue;");
                        marshalDestination = "returnValue";
                    }
                    else
                    {
                        marshalDestination = parameter.GetParameterName();
                    }

                    translator.ExportFromNative(builder,
                        parameter,
                        parameter.SourceName,
                        $"{marshalDestination} =",
                        "ParamsBuffer",
                        $"{_function.SourceName}_{parameter.SourceName}_Offset",
                        true,
                        parameter.HasAllFlags(EPropertyFlags.ReferenceParm) &&
                        !parameter.HasAllFlags(EPropertyFlags.ReturnParm));
                });
            }
            
            builder.AppendLine();
            
            ForEachParameter((translator, parameter) =>
            {
                if (!parameter.HasAnyFlags(EPropertyFlags.ReturnParm | EPropertyFlags.OutParm))
                {
                    translator.ExportCleanupMarshallingBuffer(builder, parameter, parameter.SourceName);
                }
            });
            
            if (_function.ReturnProperty != null)
            {
                builder.AppendLine("return returnValue;");
            }
        }
    }

    void ExportSignature(GeneratorStringBuilder builder, string protection)
    {
        builder.AppendTooltip(_function);

        AttributeBuilder attributeBuilder = AttributeBuilder.CreateAttributeBuilder(_function);
        
        if (BlueprintEvent)
        {
            attributeBuilder.AddArgument("FunctionFlags.BlueprintEvent");
        }
        
        attributeBuilder.AddGeneratedTypeAttribute(_function);
        attributeBuilder.Finish();
        builder.AppendLine(attributeBuilder.ToString());
        
        string returnType = _function.ReturnProperty != null
            ? ReturnValueTranslator!.GetManagedType(_function.ReturnProperty)
            : "void";
        builder.AppendLine($"{protection}{returnType} {_functionName}({_paramStringApiWithDefaults})");
    }
    

    void ExportDeprecation(GeneratorStringBuilder builder)
    {
        if (_function.HasMetadata("DeprecatedFunction"))
        {
            string deprecationMessage = _function.GetMetadata("DeprecationMessage");
            if (deprecationMessage.Length == 0)
            {
                deprecationMessage = "This function is deprecated.";
            }
            else
            {
                // Remove nested quotes
                deprecationMessage = deprecationMessage.Replace("\"", "");
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
                else if (_function.HasAllFlags(EFunctionFlags.Protected) || _function.HasMetadata("BlueprintProtected"))
                {
                    _modifiers = "protected ";
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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
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
    InternalWhitelisted,
    GetterSetter,
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
    GetterSetter,
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
    protected static readonly ConcurrentDictionary<UhtPackage, List<ExtensionMethod>> ExtensionMethods = new();
    
    protected readonly UhtFunction _function;
    protected string _functionName = null!;
    protected List<PropertyTranslator> _parameterTranslators = null!;
    protected PropertyTranslator? ReturnValueTranslator => _function.ReturnProperty != null ? _parameterTranslators.Last() : null;
    protected OverloadMode _overloadMode = OverloadMode.AllowOverloads;
    protected EFunctionProtectionMode _protectionMode = EFunctionProtectionMode.UseUFunctionProtection;
    protected EBlueprintVisibility _blueprintVisibility = EBlueprintVisibility.Call;

    protected bool BlittableFunction;
    
    public string Modifiers { get; private set; } = "";
    
    protected bool BlueprintEvent => _blueprintVisibility == EBlueprintVisibility.Event;
    protected string _invokeFunction = "";
    protected string _invokeFirstArgument = "";

    protected string _customInvoke = "";

    protected string _paramStringApiWithDefaults = "";
    protected string _paramsStringCall = "";

    protected bool _hasGenericTypeSupport = false;
    protected bool _hasCustomStructParamSupport = false;

    protected List<string> _customStructParamTypes = null!;

    protected readonly UhtProperty? _selfParameter;
    protected readonly UhtClass? _classBeingExtended;

    protected readonly List<FunctionOverload> _overloads = new();
    
    public FunctionExporter(ExtensionMethod extensionMethod)
    {
        _selfParameter = extensionMethod.SelfParameter;
        _function = extensionMethod.Function;
        _classBeingExtended = extensionMethod.Class;
    }

    public FunctionExporter(UhtFunction function) 
    {
        _function = function;
    }
    
    string GetRefQualifier(UhtProperty parameter)
    {
        if (parameter.HasAllFlags(EPropertyFlags.ConstParm))
        {
            return "";
        }
        
        if (parameter.HasAllFlags(EPropertyFlags.ReferenceParm))
        {
            return "ref ";
        }
        
        if (parameter.HasAllFlags(EPropertyFlags.OutParm))
        {
            return "out ";
        }

        return "";
    }

    public void Initialize(OverloadMode overloadMode, EFunctionProtectionMode protectionMode, EBlueprintVisibility blueprintVisibility, bool withGenerics = false)
    {
        _functionName = protectionMode != EFunctionProtectionMode.OverrideWithInternal
            ? _function.GetFunctionName()
            : _function.SourceName;
        
        _overloadMode = overloadMode;
        _protectionMode = protectionMode;
        _blueprintVisibility = blueprintVisibility;

        _parameterTranslators = new List<PropertyTranslator>(_function.Children.Count);
        
        bool isBlittable = true;
        foreach (UhtProperty parameter in _function.Properties)
        {
            PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(parameter)!;
            _parameterTranslators.Add(translator);

            if (!translator.IsBlittable && isBlittable)
            {
                isBlittable = false;
            }
        }
        BlittableFunction = isBlittable;

        _hasGenericTypeSupport = _function.HasGenericTypeSupport();

        _hasCustomStructParamSupport = _function.HasCustomStructParamSupport();

        DetermineProtectionMode();
        _invokeFunction = DetermineInvokeFunction();

        if (_function.HasAllFlags(EFunctionFlags.Static))
        {
            Modifiers += "static ";
            _invokeFirstArgument = "NativeClassPtr";
        }
        else if (_function.HasAllFlags(EFunctionFlags.Delegate))
        {
            if (_function.HasParametersOrReturnValue())
            {
                _customInvoke = "ProcessDelegate(paramsBuffer);";
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
                Modifiers += "virtual ";
            }
		
            if (_function.IsInterfaceFunction())
            {
                Modifiers = ScriptGeneratorUtilities.PublicKeyword;
            }
            
            _invokeFirstArgument = "NativeObject";
        }

        string paramString = "";
        bool hasDefaultParameters = false;

        if (_selfParameter != null)
        {
            PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(_selfParameter)!;
            string paramType = _classBeingExtended != null
                ? _classBeingExtended.GetFullManagedName()
                : translator.GetManagedType(_selfParameter);
            
            paramString = $"this {paramType} {_selfParameter.GetParameterName()}, ";
            _paramStringApiWithDefaults = paramString;
        }
        
        string paramsStringCallNative = "";

        string paramsStringCallGenerics = "";
        string paramStringApiWithDefaultsWithGenerics = "";

        bool hasGenericClassParam = false;

        _customStructParamTypes = _function.GetCustomStructParamTypes();
        
        for (int i = 0; i < _function.Children.Count; i++)
        {
            UhtProperty parameter = (UhtProperty) _function.Children[i];
            if (parameter.HasAllFlags(EPropertyFlags.ReturnParm))
            {
                continue;
            }

            PropertyTranslator translator = _parameterTranslators[i];
            
            string refQualifier = GetRefQualifier(parameter);
            string parameterName = GetParameterName(parameter);
            string parameterManagedType = translator.GetManagedType(parameter);

            if (!translator.ShouldBeDeclaredAsParameter)
            {
                continue;
            }
            
            if (_selfParameter == parameter)
            {
                if (string.IsNullOrEmpty(paramsStringCallGenerics))
                {
                    paramsStringCallGenerics += refQualifier + parameterName;
                }
                else
                {
                    paramsStringCallGenerics = $"{parameterName},  " + _paramsStringCall.Substring(0, _paramsStringCall.Length - 2);
                }

                if (string.IsNullOrEmpty(_paramsStringCall))
                {
                    _paramsStringCall += refQualifier + parameterName;
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
                bool isGenericClassParam = _hasGenericTypeSupport && parameter.IsGenericType() && parameter is UhtClassProperty;

                if (cppDefaultValue == "()" && parameter is UhtStructProperty structProperty)
                {
                    _paramsStringCall += $"new {structProperty.ScriptStruct.GetFullManagedName()}()";
                    paramsStringCallGenerics += $"new {structProperty.ScriptStruct.GetFullManagedName()}()";
                }
                else if (isGenericClassParam)
                {
                    _paramsStringCall += $"{refQualifier}{parameterName}";
                    paramsStringCallGenerics += $"typeof(DOT)";

                    hasGenericClassParam = true;
                }
                else
                {
                    _paramsStringCall += $"{refQualifier}{parameterName}";
                    paramsStringCallGenerics += $"{refQualifier}{parameterName}";
                }

                paramsStringCallNative += $"{refQualifier}{parameterName}";
                paramString += $"{refQualifier}{parameterManagedType} {parameterName}";

                if (!isGenericClassParam)
                {
                    paramStringApiWithDefaultsWithGenerics += $"{refQualifier}{parameterManagedType} {parameterName}";
                }

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
                        _paramStringApiWithDefaults += $"{refQualifier}{parameterManagedType} {parameterName}{defaultValue}";
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

                if (!isGenericClassParam)
                {
                    paramStringApiWithDefaultsWithGenerics += ", ";
                }
            }
            
            _paramsStringCall += ", ";
            paramsStringCallGenerics += ", ";
            paramsStringCallNative += ", ";
        }

        if (_selfParameter == null)
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

        if (paramsStringCallGenerics.Length > 0)
        {
            paramsStringCallGenerics = paramsStringCallGenerics[..^2];
        }

        if (paramStringApiWithDefaultsWithGenerics.Length > 0)
        {
            paramStringApiWithDefaultsWithGenerics = paramStringApiWithDefaultsWithGenerics[..^2];
        }

        if (hasGenericClassParam)
        {
            FunctionOverload overload = new FunctionOverload
            {
                ParamStringApiWithDefaults = paramStringApiWithDefaultsWithGenerics,
                ParamsStringCall = paramsStringCallGenerics,
            };

            _overloads.Add(overload);
        }
    }
    
    protected virtual string GetParameterName(UhtProperty parameter)
    {
        return parameter.GetParameterName();
    }
    
    public static void TryAddExtensionMethod(UhtFunction function)
    {
        if (!function.HasMetadata("ExtensionMethod") && !function.IsAutocast())
        {
            return;
        }
        
        UhtPackage package = function.Outer!.Package;
        
        if (!ExtensionMethods.TryGetValue(package, out var extensionMethods))
        {
            extensionMethods = new List<ExtensionMethod>();
            ExtensionMethods.TryAdd(package, extensionMethods);
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
        else if (functionType == FunctionType.GetterSetter)
        {
            protectionMode = EFunctionProtectionMode.OverrideWithInternal;
            overloadMode = OverloadMode.SuppressOverloads;
            blueprintVisibility = EBlueprintVisibility.GetterSetter;
        }
        
        builder.TryAddWithEditor(function);
        FunctionExporter exporter = new FunctionExporter(function);
        exporter.Initialize(overloadMode, protectionMode, blueprintVisibility);
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
            else if (!parameter.HasAnyFlags(EPropertyFlags.ConstParm)
                    && !parameter.HasAnyFlags(EPropertyFlags.ReferenceParm)
                    && parameter.HasAnyFlags(EPropertyFlags.OutParm))
            {
                builder.AppendLine($"{paramType} {parameter.SourceName} = default;");
            }
            else
            {
                string assignmentOrReturn = $"{paramType} {parameter.SourceName} = ";
                string offsetName = parameter.GetOffsetVariableName();
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
                string offsetName = parameter.GetOffsetVariableName();
                translator.ExportToNative(builder, parameter, parameter.SourceName, "buffer", offsetName, parameter.SourceName);
            }
        }
        
        builder.EndUnsafeBlock();
        builder.CloseBrace();
        
        builder.TryEndWithEditor(function);
        
        builder.AppendLine();
    }

    public static FunctionExporter ExportDelegateSignature(GeneratorStringBuilder builder, UhtFunction function, string delegateName)
    {
        FunctionExporter exporter = new FunctionExporter(function);
        exporter.Initialize(OverloadMode.SuppressOverloads, EFunctionProtectionMode.UseUFunctionProtection, EBlueprintVisibility.Call);

        AttributeBuilder attributeBuilder = new AttributeBuilder();
        attributeBuilder.AddGeneratedTypeAttribute(function);
        
        if (function.HasAllFlags(EFunctionFlags.MulticastDelegate))
        {
            attributeBuilder.AddAttribute("UMultiDelegate");
        }
        else
        {
            attributeBuilder.AddAttribute("USingleDelegate");
        }
        
        attributeBuilder.Finish();
        builder.AppendLine(attributeBuilder.ToString());
        
        builder.AppendLine($"public delegate void {delegateName}({exporter._paramStringApiWithDefaults});");
        builder.AppendLine();
        
        return exporter;
    }

    public static void ExportDelegateGlue(GeneratorStringBuilder builder, FunctionExporter exporter)
    {
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

        FunctionExporter exporter = new FunctionExporter(function);
        exporter.Initialize(OverloadMode.SuppressOverloads, EFunctionProtectionMode.UseUFunctionProtection, EBlueprintVisibility.Call);
        exporter.ExportSignature(builder, ScriptGeneratorUtilities.PublicKeyword);
        builder.Append(";");
        
        builder.TryEndWithEditor(function);
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
        
        string functionNameToUse = _function.IsAutocast() ? _function.GetBlueprintAutocastName() : _functionName;
        
        builder.AppendLine($"{Modifiers}{returnManagedType} {functionNameToUse}({_paramStringApiWithDefaults})");
        builder.OpenBrace();
        string returnStatement = _function.ReturnProperty != null ? "return " : "";
        UhtClass functionOwner = (UhtClass) _function.Outer!;

        string fullClassName = functionOwner.GetFullManagedName();
        builder.AppendLine($"{returnStatement}{fullClassName}.{_functionName}({_paramsStringCall});");
        builder.CloseBrace();
    }

    public void ExportFunctionVariables(GeneratorStringBuilder builder)
    {
        builder.AppendLine($"// {_function.SourceName}");
        
        string staticDeclaration = BlueprintEvent ? "" : "static ";
        builder.AppendLine($"{staticDeclaration}IntPtr {_function.SourceName}_NativeFunction;");
        
        if (_function.HasParametersOrReturnValue())
        {
            if (_hasCustomStructParamSupport)
            {
                string genericTypes = string.Join(", ", _customStructParamTypes);
                builder.AppendLine($"static int {_function.SourceName}_NativeParamsSize;");
                builder.AppendLine($"static int {_function.SourceName}_ParamsSize<{genericTypes}>()");
                builder.Indent();
                foreach (string genericType in _customStructParamTypes)
                {
                    builder.AppendLine($"where {genericType}: MarshalledStruct<{genericType}>");
                }
                List<string> variableNames = new List<string>{$"{_function.SourceName}_NativeParamsSize"};
                int customStructureParamIndex = 0;
                ForEachParameter((translator, parameter) =>
                {
                    if (!parameter.IsCustomStructureType()) return;
                    variableNames.Add($"{_customStructParamTypes[customStructureParamIndex]}.GetNativeDataSize()");
                    customStructureParamIndex++;
                });
                builder.AppendLine($"=> {string.Join(" + ", variableNames)};");
                builder.UnIndent();
                builder.AppendLine($"static IntPtr[] {_function.SourceName}_CustomStructureNativeProperties;");
            }
            else
            {
                builder.AppendLine($"static int {_function.SourceName}_ParamsSize;");
            }
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

            List<string> genericTypes = new List<string>();
            List<string> genericConstraints = new List<string>();
            if (_hasGenericTypeSupport)
            {
                genericTypes.Add("DOT");
                genericConstraints.Add(_function.GetGenericTypeConstraint());
            }

            if (_hasCustomStructParamSupport)
            {
                genericTypes.AddRange(_customStructParamTypes);
                genericConstraints.AddRange(_customStructParamTypes.ConvertAll(paramType => $"MarshalledStruct<{paramType}>"));
            }

            string genericTypeString = string.Join(", ", genericTypes);

            if (genericTypes.Count > 0)
            {
                builder.AppendLine($"{Modifiers}{returnType} {_functionName}<{genericTypeString}>({overload.ParamStringApiWithDefaults})");
                builder.Indent();
                foreach (var (genericType, constraint) in genericTypes.Zip(genericConstraints))
                    builder.AppendLine($"where {genericType} : {constraint}");
                builder.UnIndent();
            }
            else
            {
                builder.AppendLine($"{Modifiers}{returnType} {_functionName}({overload.ParamStringApiWithDefaults})");
            }

            builder.OpenBrace();
            overload.Translator?.ExportCppDefaultParameterAsLocalVariable(builder, overload.CSharpParamName, overload.CppDefaultValue, _function, overload.Parameter);

            if (genericTypes.Count > 0)
            {
                builder.AppendLine($"{returnStatement}{_functionName}<{genericTypeString}>({overload.ParamsStringCall});");
            }
            else
            {
                builder.AppendLine($"{returnStatement}{_functionName}({overload.ParamsStringCall});");
            }
            builder.CloseBrace();
        }
    }
    
    void ExportFunction(GeneratorStringBuilder builder)
    {
        builder.AppendLine();
        ExportDeprecation(builder);
        ExportSpecializationGetter(builder);
        
        ExportSignature(builder, Modifiers);
        builder.OpenBrace();
        builder.BeginUnsafeBlock();
        
        ExportInvoke(builder);
        
        builder.CloseBrace();
        builder.EndUnsafeBlock();
        builder.AppendLine();
    }

    public virtual void ExportInvoke(GeneratorStringBuilder builder)
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
                builder.AppendLine($"{_invokeFunction}({_invokeFirstArgument}, {nativeFunctionIntPtr}, {ScriptGeneratorUtilities.IntPtrZero}, {ScriptGeneratorUtilities.IntPtrZero});");
            }
            else
            {
                builder.AppendLine(_customInvoke);
            }
        }
        else
        {
            if (_hasCustomStructParamSupport)
            {
                string genericTypes = string.Join(", ", _customStructParamTypes);
                builder.AppendLine($"IntPtr Specialization = {_function.SourceName}_GetSpecialization<{genericTypes}>();");
                builder.AppendStackAllocFunction($"{_function.SourceName}_ParamsSize<{genericTypes}>()",
                    "Specialization");
            }
            else
            {
                builder.AppendStackAllocFunction($"{_function.SourceName}_ParamsSize", 
                    nativeFunctionIntPtr, 
                    !BlittableFunction);
            }

            ForEachParameter((translator, parameter) =>
            {
                if (parameter.HasAllFlags(EPropertyFlags.ReturnParm))
                {
                    return;
                }
                
                string propertyName = GetParameterName(parameter);
                
                if (parameter.HasAllFlags(EPropertyFlags.ReferenceParm) || !parameter.HasAllFlags(EPropertyFlags.OutParm))
                {
                    string offsetName = TryAddPrecedingCustomStructParams(parameter, parameter.GetOffsetVariableName());
                    translator.ExportToNative(builder, parameter, parameter.SourceName, "paramsBuffer", offsetName, propertyName);
                }
            });
            
            builder.AppendLine();

            if (string.IsNullOrEmpty(_customInvoke))
            {
                string invokedFunctionIntPtr = _hasCustomStructParamSupport ? "Specialization" : nativeFunctionIntPtr;
                
                string returnValueAddressStr = _function.ReturnProperty != null
                    ? $"paramsBuffer + {TryAddPrecedingCustomStructParams(_function.ReturnProperty, _function.ReturnProperty.GetOffsetVariableName())}"
                    : ScriptGeneratorUtilities.IntPtrZero;
                
                builder.AppendLine($"{_invokeFunction}({_invokeFirstArgument}, {invokedFunctionIntPtr}, paramsBuffer, {returnValueAddressStr});");
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
                        marshalDestination = MakeOutMarshalDestination(parameter, translator, builder);
                    }

                    string offsetName = TryAddPrecedingCustomStructParams(parameter, parameter.GetOffsetVariableName());

                    translator.ExportFromNative(builder,
                        parameter,
                        parameter.SourceName,
                        $"{marshalDestination} =",
                        "paramsBuffer",
                        offsetName,
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
            
            ExportReturnStatement(builder);
        }
    }
    
    protected virtual string MakeOutMarshalDestination(UhtProperty parameter, PropertyTranslator propertyTranslator, GeneratorStringBuilder builder)
    {
        return GetParameterName(parameter);
    }
    
    protected virtual void ExportReturnStatement(GeneratorStringBuilder builder)
    {
        if (_function.ReturnProperty != null)
        {
            builder.AppendLine("return returnValue;");
        }
    }

    void ExportSignature(GeneratorStringBuilder builder, string protection)
    {
        builder.AppendTooltip(_function);

        AttributeBuilder attributeBuilder = new AttributeBuilder(_function);
        
        if (BlueprintEvent)
        {
            attributeBuilder.AddArgument("FunctionFlags.BlueprintEvent");
        }
        
        attributeBuilder.AddGeneratedTypeAttribute(_function);

        if (_hasGenericTypeSupport)
        {
            if (_function.HasMetadata("DeterminesOutputType"))
            {
                attributeBuilder.AddAttribute("UMetaData");
                attributeBuilder.AddArgument($"\"DeterminesOutputType\"");
                attributeBuilder.AddArgument($"\"{_function.GetMetadata("DeterminesOutputType")}\"");
            }

            if (_function.HasMetadata("DynamicOutputParam"))
            {
                attributeBuilder.AddAttribute("UMetaData");
                attributeBuilder.AddArgument($"\"DynamicOutputParam\"");
                attributeBuilder.AddArgument($"\"{_function.GetMetadata("DynamicOutputParam")}\"");
            }
        }

        if (_hasCustomStructParamSupport)
        {
            attributeBuilder.AddAttribute("UMetaData");
            attributeBuilder.AddArgument($"\"CustomStructureParam\"");
            attributeBuilder.AddArgument($"\"{_function.GetMetadata("CustomStructureParam")}\"");
        }

        attributeBuilder.Finish();
        builder.AppendLine(attributeBuilder.ToString());

        string returnType = _function.ReturnProperty != null
            ? ReturnValueTranslator!.GetManagedType(_function.ReturnProperty)
            : "void";

        List<string> genericTypes = new List<string>();
        List<string> genericConstraints = new List<string>();
        if (_hasGenericTypeSupport)
        {
            genericTypes.Add("DOT");
            genericConstraints.Add(_function.GetGenericTypeConstraint());
        }

        if (_hasCustomStructParamSupport)
        {
            genericTypes.AddRange(_customStructParamTypes);
            genericConstraints.AddRange(_customStructParamTypes.ConvertAll(paramType => $"MarshalledStruct<{paramType}>"));
        }

        if (genericTypes.Count > 0)
        {
            builder.AppendLine($"{protection}{returnType} {_functionName}<{string.Join(", ", genericTypes)}>({_paramStringApiWithDefaults})");
            builder.Indent();
            foreach (var (genericType, constraint) in genericTypes.Zip(genericConstraints))
                builder.AppendLine($"where {genericType} : {constraint}");
            builder.UnIndent();
        }
        else
        {
            builder.AppendLine($"{protection}{returnType} {_functionName}({_paramStringApiWithDefaults})");
        }
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

    void ExportSpecializationGetter(GeneratorStringBuilder builder)
    {
        if (_hasCustomStructParamSupport)
        {
            int customStructureParamCount = _customStructParamTypes.Count;
            string dictionaryKey = customStructureParamCount == 1
                ? "IntPtr"
                : $"({string.Join(", ", Enumerable.Repeat("IntPtr", customStructureParamCount))})";
            builder.AppendLine($"static Dictionary<{dictionaryKey}, IntPtr> {_function.SourceName}_Specializations = new Dictionary<{dictionaryKey}, IntPtr>();");
            builder.AppendLine($"static IntPtr {_function.SourceName}_GetSpecialization<{string.Join(", ", _customStructParamTypes)}>()");
            builder.Indent();
            foreach (string customStructParamType in _customStructParamTypes)
            {
                builder.AppendLine($"where {customStructParamType} : MarshalledStruct<{customStructParamType}>");
            }
            builder.UnIndent();
            builder.OpenBrace();
            builder.AppendLine("IntPtr specializationNativeFunction;");
            List<string> nativeClassPtrs = _customStructParamTypes.ConvertAll(customStructParamType =>
                $"{customStructParamType}.GetNativeClassPtr()");
            string specializationKeyInitializer = nativeClassPtrs.Count == 1 ? nativeClassPtrs[0] : $"({string.Join(", ", nativeClassPtrs)})";
            builder.AppendLine($"{dictionaryKey} specializationKey = {specializationKeyInitializer};");
            builder.AppendLine($"if(!{_function.SourceName}_Specializations.TryGetValue(specializationKey, out specializationNativeFunction))");
            builder.OpenBrace();
            builder.BeginUnsafeBlock();
            string customStructBufferInitializationList = customStructureParamCount == 1 ? "specializationKey" : string.Join(", ", Enumerable.Range(1, customStructureParamCount).ToList().ConvertAll(i => $"specializationKey.Item{i}"));
            builder.AppendLine($"IntPtr* customStructBufferAllocation = stackalloc IntPtr[]{{{customStructBufferInitializationList}}};");
            builder.AppendLine("IntPtr customStructBuffer = (IntPtr) customStructBufferAllocation;");
            string nativeFunctionIntPtr = $"{_function.SourceName}_NativeFunction";
            string customStructNativePropertiesIntPtr = $"{_function.SourceName}_CustomStructureNativeProperties";
            builder.AppendLine($"fixed(nint* nativePropertyBuffer = {customStructNativePropertiesIntPtr})");
            builder.OpenBrace();
            builder.AppendLine($"specializationNativeFunction = {ExporterCallbacks.UFunctionCallbacks}.CallCreateNativeFunctionCustomStructSpecialization({nativeFunctionIntPtr}, (nint) nativePropertyBuffer, customStructBuffer);");
            builder.CloseBrace();
            builder.AppendLine($"{_function.SourceName}_Specializations.Add(specializationKey, specializationNativeFunction);");
            builder.EndUnsafeBlock();
            builder.CloseBrace();
            builder.AppendLine("return specializationNativeFunction;");
            builder.CloseBrace();
        }
    }

    void DetermineProtectionMode()
    {
        switch (_protectionMode)
        {
            case EFunctionProtectionMode.UseUFunctionProtection:
                if (_function.HasAnyFlags(EFunctionFlags.Public | EFunctionFlags.BlueprintCallable))
                {
                    Modifiers = ScriptGeneratorUtilities.PublicKeyword;
                }
                else if (_function.HasAllFlags(EFunctionFlags.Protected) || _function.HasMetadata("BlueprintProtected"))
                {
                    Modifiers = ScriptGeneratorUtilities.ProtectedKeyword;
                }
                else
                {
                    Modifiers = ScriptGeneratorUtilities.PrivateKeyword;
                }
                break;
            case EFunctionProtectionMode.OverrideWithInternal:
                Modifiers = "internal ";
                break;
        }
    }

    string DetermineInvokeFunction()
    {
        string invokeFunction = ExporterCallbacks.UObjectCallbacks;
        
        if (_function.HasAllFlags(EFunctionFlags.Static))
        {
            return invokeFunction + ".CallInvokeNativeStaticFunction";
        }
        
        if (_function.HasAllFlags(EFunctionFlags.Net))
        {
            return invokeFunction + ".CallInvokeNativeNetFunction";
        }
        
        if (_function.HasAllFlags(EFunctionFlags.HasOutParms) || _function.HasReturnProperty)
        {
            return invokeFunction + ".CallInvokeNativeFunctionOutParms";
        } 
        
        return invokeFunction + ".CallInvokeNativeFunction";
    }
    
    public string TryAddPrecedingCustomStructParams(UhtProperty parameter, string name)
    {
        if (!_hasCustomStructParamSupport)
        {
            return name;
        }
        
        int precedingCustomStructParams = parameter.GetPrecedingCustomStructParams();
        if (precedingCustomStructParams > 0)
        {
            return name + $"<{string.Join(", ", _customStructParamTypes.GetRange(0, precedingCustomStructParams))}>()";
        }
        
        return name;
    }
}

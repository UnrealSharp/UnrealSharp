using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.Attributes;
using UnrealSharpManagedGlue.PropertyTranslators;
using UnrealSharpManagedGlue.SourceGeneration;
using UnrealSharpManagedGlue.Utilities;
using UnrealSharpManagedGlue.Tooltip;

namespace UnrealSharpManagedGlue.Exporters;

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
    
    public UhtFunction Function { get; }
    protected string FunctionName = null!;
    protected List<PropertyTranslator> ParameterTranslators = null!;
    protected PropertyTranslator? ReturnValueTranslator => Function.ReturnProperty != null ? ParameterTranslators.Last() : null;
    protected OverloadMode OverloadMode = OverloadMode.AllowOverloads;
    protected EFunctionProtectionMode ProtectionMode = EFunctionProtectionMode.UseUFunctionProtection;
    protected EBlueprintVisibility BlueprintVisibility = EBlueprintVisibility.Call;

    protected bool BlittableFunction;
    
    public string Modifiers { get; private set; } = "";

    protected bool BlueprintEvent => Function.HasAllFlags(EFunctionFlags.BlueprintEvent);
    protected bool BlueprintNativeEvent => Function.IsBlueprintNativeEvent();
    protected bool BlueprintImplementableEvent => Function.IsBlueprintImplementableEvent();
    protected bool BlueprintCallable => Function.HasAnyFlags(EFunctionFlags.BlueprintCallable);
    
    protected string InvokeFunction = "";
    protected string InvokeFirstArgument = "";

    protected string CustomInvoke = "";

    protected string ParamStringApiWithDefaults = "";
    protected string ParamsStringCall = "";

    protected bool HasGenericTypeSupport;
    protected bool HasCustomStructParamSupport;

    protected List<string> CustomStructParamTypes = null!;

    protected readonly UhtProperty? SelfParameter;
    protected readonly UhtClass? ClassBeingExtended;

    protected readonly List<FunctionOverload> Overloads = new();
    
    public string NativeFunctionIntPtr => $"{Function.SourceName}_NativeFunction";
    public string InstanceFunctionPtr => $"{Function.SourceName}_InstanceFunction";
    
    public FunctionExporter(ExtensionMethod extensionMethod)
    {
        SelfParameter = extensionMethod.SelfParameter;
        Function = extensionMethod.Function;
        ClassBeingExtended = extensionMethod.Class;
    }

    public FunctionExporter(UhtFunction function) 
    {
        Function = function;
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

    public void Initialize(OverloadMode overloadMode, EFunctionProtectionMode protectionMode, EBlueprintVisibility blueprintVisibility)
    {
        FunctionName = protectionMode != EFunctionProtectionMode.OverrideWithInternal
            ? Function.GetFunctionName()
            : Function.SourceName;
        
        OverloadMode = overloadMode;
        ProtectionMode = protectionMode;
        BlueprintVisibility = blueprintVisibility;

        ParameterTranslators = new List<PropertyTranslator>(Function.Children.Count);
        
        bool isBlittable = true;
        foreach (UhtProperty parameter in Function.Properties)
        {
            PropertyTranslator translator = parameter.GetTranslator()!;
            ParameterTranslators.Add(translator);

            if (!translator.IsBlittable && isBlittable)
            {
                isBlittable = false;
            }
        }
        BlittableFunction = isBlittable;

        HasGenericTypeSupport = Function.HasGenericTypeSupport();

        HasCustomStructParamSupport = Function.HasCustomStructParamSupport();

        DetermineProtectionMode();
        InvokeFunction = DetermineInvokeFunction();

        if (Function.HasAllFlags(EFunctionFlags.Static))
        {
            Modifiers += "static ";
            InvokeFirstArgument = "NativeClassPtr";
        }
        else if (Function.HasAllFlags(EFunctionFlags.Delegate))
        {
            if (Function.HasParametersOrReturnValue())
            {
                CustomInvoke = "ProcessDelegate(paramsBuffer);";
            }
            else
            {
                CustomInvoke = "ProcessDelegate(IntPtr.Zero);";
            }
        }
        else
        {
            if (BlueprintEvent && !BlueprintCallable)
            {
                Modifiers += "virtual ";
            }
            
            if (Function.IsInterfaceFunction())
            {
                Modifiers = ScriptGeneratorUtilities.PublicKeyword;
            }
            
            InvokeFirstArgument = "NativeObject";
        }

        string paramString = "";
        bool hasDefaultParameters = false;

        if (SelfParameter != null)
        {
            PropertyTranslator translator = SelfParameter.GetTranslator()!;
            string paramType = ClassBeingExtended != null
                ? ClassBeingExtended.GetFullManagedName()
                : translator.GetManagedType(SelfParameter);
            
            paramString = $"this {paramType} {SelfParameter.GetParameterName()}, ";
            ParamStringApiWithDefaults = paramString;
        }
        
        string paramsStringCallNative = "";

        string paramsStringCallGenerics = "";
        string paramStringApiWithDefaultsWithGenerics = "";

        bool hasGenericClassParam = false;

        CustomStructParamTypes = Function.GetCustomStructParamTypes();
        
        for (int i = 0; i < Function.Children.Count; i++)
        {
            UhtProperty parameter = (UhtProperty) Function.Children[i];
            if (parameter.HasAllFlags(EPropertyFlags.ReturnParm))
            {
                continue;
            }

            PropertyTranslator translator = ParameterTranslators[i];
            
            string refQualifier = GetRefQualifier(parameter);
            string parameterName = GetParameterName(parameter);
            string parameterManagedType = translator.GetManagedType(parameter);

            if (!translator.ShouldBeDeclaredAsParameter)
            {
                continue;
            }
            
            if (SelfParameter == parameter)
            {
                if (string.IsNullOrEmpty(paramsStringCallGenerics))
                {
                    paramsStringCallGenerics += refQualifier + parameterName;
                }
                else
                {
                    paramsStringCallGenerics = $"{parameterName},  " + ParamsStringCall.Substring(0, ParamsStringCall.Length - 2);
                }

                if (string.IsNullOrEmpty(ParamsStringCall))
                {
                    ParamsStringCall += refQualifier + parameterName;
                }
                else
                {
                    ParamsStringCall = $"{parameterName},  " + ParamsStringCall.Substring(0, ParamsStringCall.Length - 2);
                }
                paramsStringCallNative += parameterName;
            }
            else
            {
                string cppDefaultValue = translator.GetCppDefaultValue(Function, parameter);
                bool isGenericClassParam = HasGenericTypeSupport && parameter.IsGenericType() && !parameter.HasAnyFlags(EPropertyFlags.OutParm) && parameter is UhtClassProperty;

                if (cppDefaultValue == "()" && parameter is UhtStructProperty structProperty)
                {
                    ParamsStringCall += $"new {structProperty.ScriptStruct.GetFullManagedName()}()";
                    paramsStringCallGenerics += $"new {structProperty.ScriptStruct.GetFullManagedName()}()";
                }
                else if (isGenericClassParam)
                {
                    ParamsStringCall += $"{refQualifier}{parameterName}";
                    paramsStringCallGenerics += $"typeof(DOT)";

                    hasGenericClassParam = true;
                }
                else
                {
                    ParamsStringCall += $"{refQualifier}{parameterName}";
                    paramsStringCallGenerics += $"{refQualifier}{parameterName}";
                }

                paramsStringCallNative += $"{refQualifier}{parameterName}";
                paramString += $"{refQualifier}{parameterManagedType} {parameterName}";

                if (!isGenericClassParam)
                {
                    paramStringApiWithDefaultsWithGenerics += $"{refQualifier}{parameterManagedType} {parameterName}";
                }

                if ((hasDefaultParameters || cppDefaultValue.Length > 0) && OverloadMode == OverloadMode.AllowOverloads)
                {
                    hasDefaultParameters = true;
                    string csharpDefaultValue = "";
                    
                    if (cppDefaultValue.Length == 0 || cppDefaultValue == "None")
                    {
                        csharpDefaultValue = translator.GetNullValue(parameter);
                    }
                    else if (translator.ExportDefaultParameter)
                    {
                        csharpDefaultValue = translator.ConvertCppDefaultValue(cppDefaultValue, Function, parameter);
                    }
                    
                    if (!string.IsNullOrEmpty(csharpDefaultValue))
                    {
                        string defaultValue = $" = {csharpDefaultValue}";
                        ParamStringApiWithDefaults += $"{refQualifier}{parameterManagedType} {parameterName}{defaultValue}";
                    }
                    else
                    {
                        if (ParamStringApiWithDefaults.Length > 0)
                        {
                            ParamStringApiWithDefaults = ParamStringApiWithDefaults.Substring(0, ParamStringApiWithDefaults.Length - 2);
                        }

                        FunctionOverload overload = new FunctionOverload
                        {
                            ParamStringApiWithDefaults = ParamStringApiWithDefaults,
                            ParamsStringCall = ParamsStringCall,
                            CSharpParamName = parameterName,
                            CppDefaultValue = cppDefaultValue,
                            Translator = translator,
                            Parameter = parameter,
                        };
                        
                        Overloads.Add(overload);

                        ParamStringApiWithDefaults = paramString;
                    }
                }
                else
                {
                    ParamStringApiWithDefaults = paramString;
                }
                
                paramString += ", ";
                ParamStringApiWithDefaults += ", ";

                if (!isGenericClassParam)
                {
                    paramStringApiWithDefaultsWithGenerics += ", ";
                }
            }
            
            ParamsStringCall += ", ";
            paramsStringCallGenerics += ", ";
            paramsStringCallNative += ", ";
        }

        if (SelfParameter == null)
        {
            ParamsStringCall = paramsStringCallNative;
        }
        
        // remove last comma
        if (ParamStringApiWithDefaults.Length > 0)
        {
            ParamStringApiWithDefaults = ParamStringApiWithDefaults.Substring(0, ParamStringApiWithDefaults.Length - 2);
        }  
        
        if (ParamsStringCall.Length > 0)
        {
            ParamsStringCall = ParamsStringCall.Substring(0, ParamsStringCall.Length - 2);
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

            Overloads.Add(overload);
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

    public static void StartExportingExtensionMethods()
    {
        foreach (KeyValuePair<UhtPackage, List<ExtensionMethod>> extensionInfo in ExtensionMethods)
        {
            TaskManager.StartTask(_ =>
            {
                ExtensionsClassExporter.ExportExtensionsClass(extensionInfo.Key, extensionInfo.Value); 
            });
        }
    }
    
    public static FunctionExporter ExportFunction(GeneratorStringBuilder builder, UhtFunction function, FunctionType functionType, HashSet<string>? exportedFunctions = null)
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
        if (exportedFunctions is null || !exportedFunctions.Contains(function.SourceName))
        {
            exporter.ExportFunctionVariables(builder);
        }

        exporter.ExportOverloads(builder);
        exporter.ExportFunction(builder);

        builder.TryEndWithEditor(function);

        return exporter;
    }
    
    public static void ExportOverridableFunction(GeneratorStringBuilder builder, UhtFunction function, HashSet<string>? exportedFunctions = null)
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

            string paramName = parameter.GetParameterName();
            string paramType = parameter.GetTranslator()!.GetManagedType(parameter);
            
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
        
        FunctionExporter exportFunction = ExportFunction(builder, function, FunctionType.BlueprintEvent, exportedFunctions);
        
        string returnType = function.ReturnProperty != null
            ? function.ReturnProperty.GetTranslator()!.GetManagedType(function.ReturnProperty)
            : "void";
        
        AttributeBuilder attributeBuilder = new AttributeBuilder(function);
        attributeBuilder.AddGeneratedTypeAttribute(function);
        attributeBuilder.Finish();
        builder.AppendLine(attributeBuilder.ToString());

        if (function.HasAnyFlags(EFunctionFlags.BlueprintCallable))
        {
            builder.AppendLine($"protected virtual {returnType} {methodName}_Implementation({paramsStringApi})");
        
            builder.OpenBrace();
            if (exportFunction.BlueprintNativeEvent)
            {
                exportFunction.ExportInvoke(builder);
            }
            else
            {
                exportFunction.ForEachParameter((translator, parameter) =>
                {
                    if (!parameter.HasAllFlags(EPropertyFlags.OutParm) || parameter.HasAnyFlags(EPropertyFlags.ReturnParm | EPropertyFlags.ConstParm | EPropertyFlags.ReferenceParm))
                    {
                        return;
                    }
                
                    string paramName = parameter.GetParameterName();
                    string nullValue = translator.GetNullValue(parameter);
                    builder.AppendLine($"{paramName} = {nullValue};");
                });

                if (function.ReturnProperty != null)
                {
                    PropertyTranslator translator = function.ReturnProperty.GetTranslator()!;
                    string nullValue = translator.GetNullValue(function.ReturnProperty);
                    builder.AppendLine($"return {nullValue};");
                }
            }
            builder.CloseBrace();
        }
        
        builder.AppendLine($"void Invoke_{function.EngineName}(IntPtr buffer, IntPtr returnBuffer)");
        builder.OpenBrace();
        builder.BeginUnsafeBlock();
        
        string returnAssignment = "";
        exportFunction.ForEachParameter((translator, parameter) =>
        {
            string paramType = translator.GetManagedType(parameter);

            if (parameter.HasAllFlags(EPropertyFlags.ReturnParm))
            {
                returnAssignment = $"{paramType} returnValue = ";
            }
            else if (!parameter.HasAnyFlags(EPropertyFlags.ConstParm)
                     && !parameter.HasAnyFlags(EPropertyFlags.ReferenceParm)
                     && parameter.HasAnyFlags(EPropertyFlags.OutParm))
            {
                builder.AppendLine($"{paramType} {parameter.GetParameterName()} = default;");
            }
            else
            {
                string parameterName = parameter.GetParameterName();
                string assignmentOrReturn = $"{paramType} {parameterName} = ";
                string offsetName = parameter.GetOffsetVariableName();

                translator.ExportFromNative(builder, parameter, parameter.SourceName, assignmentOrReturn, "buffer",
                    offsetName, false, false);
            }
        });
        
        string implementationFunctionName = function.HasAnyFlags(EFunctionFlags.BlueprintCallable)
            ? "_Implementation"
            : string.Empty;
        
        builder.AppendLine($"{returnAssignment}{methodName}{implementationFunctionName}({paramsCallString});");

        if (function.ReturnProperty != null)
        {
            PropertyTranslator translator = function.ReturnProperty.GetTranslator()!;
            translator.ExportToNative(builder, function.ReturnProperty, function.ReturnProperty.SourceName, "returnBuffer", "0", "returnValue");
        }

        exportFunction.ForEachParameter((translator, parameter) =>
        {
            if (parameter.HasAnyFlags(EPropertyFlags.ReturnParm | EPropertyFlags.ConstParm) || !parameter.HasAnyFlags(EPropertyFlags.OutParm))
            {
                return;
            }

            translator.ExportToNative(builder, parameter, parameter.SourceName, "buffer", parameter.GetOffsetVariableName(), parameter.GetParameterName());
        });
        
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
        // Use specialized delegate attribute method with modified C# delegate name (including Outer prefix)
        attributeBuilder.AddGeneratedDelegateTypeAttribute(function, delegateName);
        
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
        
        builder.AppendLine($"public delegate void {delegateName}({exporter.ParamStringApiWithDefaults});");
        builder.AppendLine();
        
        return exporter;
    }

    public static void ExportDelegateGlue(GeneratorStringBuilder builder, FunctionExporter exporter, string delegateName)
    {
        exporter.ExportFunctionVariables(builder);
        builder.AppendLine();
        
        builder.AppendLine($"protected override {delegateName} GetInvoker() => Invoker;");
        builder.AppendLine($"protected void Invoker({exporter.ParamStringApiWithDefaults})");
        
        builder.OpenBrace();
        exporter.ExportInvoke(builder);
        builder.CloseBrace();
    }

    public static void ExportDelegateExtensions(GeneratorStringBuilder builder, FunctionExporter exporter, string delegateName)
    {
        builder.AppendLine($"public static class {exporter.FunctionName}Extensions");
        builder.OpenBrace();

        string signatureParams = exporter.Function.HasParameters ? ", " + exporter.ParamStringApiWithDefaults : string.Empty;
        string invokeArgs = exporter.Function.HasParameters ? exporter.ParamsStringCall : string.Empty;
        builder.AppendLine($"public static void Invoke(this T{delegateName} del{signatureParams}) => del.InnerDelegate.Invoke({invokeArgs});");

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
        for (int i = 0; i < Function.Children.Count; i++)
        {
            UhtProperty parameter = (UhtProperty) Function.Children[i];
            PropertyTranslator translator = ParameterTranslators[i];
            action(translator, parameter);
        }
    }
    
    public void ExportExtensionMethodOverloads(GeneratorStringBuilder builder)
    {
        foreach (FunctionOverload overload in Overloads)
        {
            builder.AppendLine();
            ExportDeprecation(builder);

            string returnType = "void";
            string returnStatement = "";
            if (Function.ReturnProperty != null)
            {
                returnType = ReturnValueTranslator!.GetManagedType(Function.ReturnProperty);
                returnStatement = "return ";
            }

            List<string> genericTypes = new List<string>();
            List<string> genericConstraints = new List<string>();
            if (HasGenericTypeSupport)
            {
                genericTypes.Add("DOT");
                genericConstraints.Add(Function.GetGenericTypeConstraint());
            }

            if (HasCustomStructParamSupport)
            {
                genericTypes.AddRange(CustomStructParamTypes);
                genericConstraints.AddRange(CustomStructParamTypes.ConvertAll(paramType => $"MarshalledStruct<{paramType}>"));
            }

            string genericTypeString = string.Join(", ", genericTypes);

            if (genericTypes.Count > 0)
            {
                PropertyTranslator translator = ParameterTranslators[0];
                string paramType = ClassBeingExtended != null ? ClassBeingExtended.GetFullManagedName() : translator.GetManagedType(SelfParameter!);
                builder.AppendLine($"{Modifiers}{returnType} {FunctionName}<{genericTypeString}>(this {paramType} {SelfParameter!.GetParameterName()}, {overload.ParamStringApiWithDefaults})");
                builder.Indent();
                
                foreach ((string genericType, string constraint) in genericTypes.Zip(genericConstraints))
                {
                    builder.AppendLine($"where {genericType} : {constraint}");
                }

                builder.UnIndent();
            }
            else
            {
                builder.AppendLine($"{Modifiers}{returnType} {FunctionName}(this {overload.ParamStringApiWithDefaults})");
            }

            builder.OpenBrace();
            overload.Translator?.ExportCppDefaultParameterAsLocalVariable(builder, overload.CSharpParamName, overload.CppDefaultValue, Function, overload.Parameter);

            UhtClass functionOwner = (UhtClass) Function.Outer!;
            string fullClassName = functionOwner.GetFullManagedName();
            if (genericTypes.Count > 0)
            {
                builder.AppendLine($"{returnStatement}{fullClassName}.{FunctionName}<{genericTypeString}>({overload.ParamsStringCall});");
            }
            else
            {
                builder.AppendLine($"{returnStatement}{fullClassName}.{FunctionName}({overload.ParamsStringCall});");
            }
            builder.CloseBrace();
        }
    }
    
    public void ExportExtensionMethod(GeneratorStringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendTooltip(Function);
        ExportDeprecation(builder);

        string returnManagedType = "void";
        if (ReturnValueTranslator != null)
        {
            returnManagedType = ReturnValueTranslator.GetManagedType(Function.ReturnProperty!);
        }
        
        string functionNameToUse = Function.IsAutocast() ? Function.GetBlueprintAutocastName() : FunctionName;
        
        List<string> genericTypes = new List<string>();
        List<string> genericConstraints = new List<string>();
        if (HasGenericTypeSupport)
        {
            genericTypes.Add("DOT");
            genericConstraints.Add(Function.GetGenericTypeConstraint());
        }

        if (HasCustomStructParamSupport)
        {
            genericTypes.AddRange(CustomStructParamTypes);
            genericConstraints.AddRange(CustomStructParamTypes.ConvertAll(paramType => $"MarshalledStruct<{paramType}>"));
        }

        string genericTypeString = string.Join(", ", genericTypes);

        if (genericTypes.Count > 0)
        {
            builder.AppendLine($"{Modifiers}{returnManagedType} {functionNameToUse}<{genericTypeString}>({ParamStringApiWithDefaults})");
            builder.Indent();
            
            foreach ((string genericType, string constraint) in genericTypes.Zip(genericConstraints))
            {
                builder.AppendLine($"where {genericType} : {constraint}");
            }

            builder.UnIndent();
        }
        else
        {
            builder.AppendLine($"{Modifiers}{returnManagedType} {functionNameToUse}({ParamStringApiWithDefaults})");
        }
        
        builder.OpenBrace();
        string returnStatement = Function.ReturnProperty != null ? "return " : "";
        UhtClass functionOwner = (UhtClass) Function.Outer!;

        string fullClassName = functionOwner.GetFullManagedName();
        builder.AppendLine($"{returnStatement}{fullClassName}.{FunctionName}({ParamsStringCall});");
        builder.CloseBrace();
    }

    public void ExportFunctionVariables(GeneratorStringBuilder builder)
    {
        builder.AppendLine($"// {Function.SourceName}");

        if (!BlueprintImplementableEvent)
        {
            builder.AppendLine($"static IntPtr {NativeFunctionIntPtr};");
        }

        if (BlueprintEvent)
        {
            builder.AppendLine($"IntPtr {InstanceFunctionPtr};");
        }
        
        if (Function.HasParametersOrReturnValue())
        {
            if (HasCustomStructParamSupport)
            {
                string genericTypes = string.Join(", ", CustomStructParamTypes);
                builder.AppendLine($"static int {Function.SourceName}_NativeParamsSize;");
                builder.AppendLine($"static int {Function.SourceName}_ParamsSize<{genericTypes}>()");
                builder.Indent();
                foreach (string genericType in CustomStructParamTypes)
                {
                    builder.AppendLine($"where {genericType}: MarshalledStruct<{genericType}>");
                }
                List<string> variableNames = new List<string>{$"{Function.SourceName}_NativeParamsSize"};
                int customStructureParamIndex = 0;
                ForEachParameter((_, parameter) =>
                {
                    if (!parameter.IsCustomStructureType()) return;
                    variableNames.Add($"{CustomStructParamTypes[customStructureParamIndex]}.GetNativeDataSize()");
                    customStructureParamIndex++;
                });
                builder.AppendLine($"=> {string.Join(" + ", variableNames)};");
                builder.UnIndent();
                builder.AppendLine($"static IntPtr[] {Function.SourceName}_CustomStructureNativeProperties;");
            }
            else
            {
                builder.AppendLine($"static int {Function.SourceName}_ParamsSize;");
            }
        }
        
        ForEachParameter((translator, parameter) =>
        {
            translator.ExportParameterVariables(builder, Function, Function.SourceName, parameter, parameter.SourceName);
        });
    }

    void ExportOverloads(GeneratorStringBuilder builder)
    {
        foreach (FunctionOverload overload in Overloads)
        {
            builder.AppendLine();
            ExportDeprecation(builder);

            string returnType = "void";
            string returnStatement = "";
            if (Function.ReturnProperty != null)
            {
                returnType = ReturnValueTranslator!.GetManagedType(Function.ReturnProperty);
                returnStatement = "return ";
            }

            List<string> genericTypes = new List<string>();
            List<string> genericConstraints = new List<string>();
            if (HasGenericTypeSupport)
            {
                genericTypes.Add("DOT");
                genericConstraints.Add(Function.GetGenericTypeConstraint());
            }

            if (HasCustomStructParamSupport)
            {
                genericTypes.AddRange(CustomStructParamTypes);
                genericConstraints.AddRange(CustomStructParamTypes.ConvertAll(paramType => $"MarshalledStruct<{paramType}>"));
            }

            string genericTypeString = string.Join(", ", genericTypes);

            if (genericTypes.Count > 0)
            {
                builder.AppendLine($"{Modifiers}{returnType} {FunctionName}<{genericTypeString}>({overload.ParamStringApiWithDefaults})");
                builder.Indent();
                foreach (var (genericType, constraint) in genericTypes.Zip(genericConstraints))
                    builder.AppendLine($"where {genericType} : {constraint}");
                builder.UnIndent();
            }
            else
            {
                builder.AppendLine($"{Modifiers}{returnType} {FunctionName}({overload.ParamStringApiWithDefaults})");
            }

            builder.OpenBrace();
            overload.Translator?.ExportCppDefaultParameterAsLocalVariable(builder, overload.CSharpParamName, overload.CppDefaultValue, Function, overload.Parameter);

            if (genericTypes.Count > 0)
            {
                builder.AppendLine($"{returnStatement}{FunctionName}<{genericTypeString}>({overload.ParamsStringCall});");
            }
            else
            {
                builder.AppendLine($"{returnStatement}{FunctionName}({overload.ParamsStringCall});");
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
        
        if (BlueprintEvent)
        {
            builder.AppendLine($"if ({InstanceFunctionPtr} == IntPtr.Zero)");
            builder.OpenBrace();

            string nativeCall = BlueprintCallable ? "GetNativeFunctionFromInstanceAndName" : "GetFirstNativeImplementationFromInstanceAndName";
            
            builder.AppendLine($"{InstanceFunctionPtr} = {ExporterCallbacks.UClassCallbacks}.Call{nativeCall}(NativeObject, \"{Function.EngineName}\");");
            builder.CloseBrace();
            
            ExportInvoke(builder, InstanceFunctionPtr);
        }
        else
        {
            ExportInvoke(builder);
        }
        
        builder.CloseBrace();
        builder.AppendLine();
    }

    public void ExportInvoke(GeneratorStringBuilder builder, string functionPtr = "")
    {
        builder.BeginUnsafeBlock();
        string nativeFunctionIntPtr = string.IsNullOrEmpty(functionPtr) ? NativeFunctionIntPtr : functionPtr;
        
        if (!Function.HasParametersOrReturnValue())
        {
            if (string.IsNullOrEmpty(CustomInvoke))
            {
                builder.AppendLine($"{InvokeFunction}({InvokeFirstArgument}, {nativeFunctionIntPtr}, {ScriptGeneratorUtilities.IntPtrZero}, {ScriptGeneratorUtilities.IntPtrZero});");
            }
            else
            {
                builder.AppendLine(CustomInvoke);
            }
        }
        else
        {
            if (HasCustomStructParamSupport)
            {
                string genericTypes = string.Join(", ", CustomStructParamTypes);
                builder.AppendLine($"IntPtr Specialization = {Function.SourceName}_GetSpecialization<{genericTypes}>();");
                builder.AppendStackAllocFunction($"{Function.SourceName}_ParamsSize<{genericTypes}>()",
                    "Specialization");
            }
            else
            {
                builder.AppendStackAllocFunction($"{Function.SourceName}_ParamsSize", 
                    nativeFunctionIntPtr, 
                    !BlittableFunction);
            }

            ForEachParameter((translator, parameter) =>
            {
                if (parameter.HasAllFlags(EPropertyFlags.ReturnParm))
                {
                    return;
                }

                if (!parameter.HasAllFlags(EPropertyFlags.ReferenceParm) && parameter.HasAllFlags(EPropertyFlags.OutParm))
                {
                    return;
                }
                
                string offsetName = TryAddPrecedingCustomStructParams(parameter, parameter.GetOffsetVariableName());
                translator.ExportToNative(builder, parameter, parameter.SourceName, "paramsBuffer", offsetName, GetParameterName(parameter));
            });
            
            builder.AppendLine();

            if (string.IsNullOrEmpty(CustomInvoke))
            {
                string invokedFunctionIntPtr = HasCustomStructParamSupport ? "Specialization" : nativeFunctionIntPtr;
                
                string returnValueAddressStr = Function.ReturnProperty != null
                    ? $"paramsBuffer + {TryAddPrecedingCustomStructParams(Function.ReturnProperty, Function.ReturnProperty.GetOffsetVariableName())}"
                    : ScriptGeneratorUtilities.IntPtrZero;
                
                builder.AppendLine($"{InvokeFunction}({InvokeFirstArgument}, {invokedFunctionIntPtr}, paramsBuffer, {returnValueAddressStr});");
            }
            else
            {
                builder.AppendLine(CustomInvoke);
            }

            if (Function.ReturnProperty != null || Function.HasOutParams())
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
                        marshalDestination = "var returnValue";
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
        builder.EndUnsafeBlock();
    }
    
    protected virtual string MakeOutMarshalDestination(UhtProperty parameter, PropertyTranslator propertyTranslator, GeneratorStringBuilder builder)
    {
        return GetParameterName(parameter);
    }
    
    protected virtual void ExportReturnStatement(GeneratorStringBuilder builder)
    {
        if (Function.ReturnProperty != null)
        {
            builder.AppendLine("return returnValue;");
        }
    }

    void ExportSignature(GeneratorStringBuilder builder, string protection)
    {
        builder.AppendTooltip(Function);

        AttributeBuilder attributeBuilder = new AttributeBuilder(Function);
        
        if (BlueprintEvent)
        {
            attributeBuilder.AddArgument("FunctionFlags.BlueprintEvent");
        }
        
        attributeBuilder.AddGeneratedTypeAttribute(Function);

        if (HasGenericTypeSupport)
        {
            if (Function.HasMetadata("DeterminesOutputType"))
            {
                attributeBuilder.AddAttribute("UMetaData");
                attributeBuilder.AddArgument($"\"DeterminesOutputType\"");
                attributeBuilder.AddArgument($"\"{Function.GetMetadata("DeterminesOutputType")}\"");
            }

            if (Function.HasMetadata("DynamicOutputParam"))
            {
                attributeBuilder.AddAttribute("UMetaData");
                attributeBuilder.AddArgument($"\"DynamicOutputParam\"");
                attributeBuilder.AddArgument($"\"{Function.GetMetadata("DynamicOutputParam")}\"");
            }
        }

        if (HasCustomStructParamSupport)
        {
            attributeBuilder.AddAttribute("UMetaData");
            attributeBuilder.AddArgument($"\"CustomStructureParam\"");
            attributeBuilder.AddArgument($"\"{Function.GetMetadata("CustomStructureParam")}\"");
        }

        attributeBuilder.Finish();
        builder.AppendLine(attributeBuilder.ToString());

        string returnType = Function.ReturnProperty != null
            ? ReturnValueTranslator!.GetManagedType(Function.ReturnProperty)
            : "void";

        List<string> genericTypes = new List<string>();
        List<string> genericConstraints = new List<string>();
        if (HasGenericTypeSupport)
        {
            genericTypes.Add("DOT");
            genericConstraints.Add(Function.GetGenericTypeConstraint());
        }

        if (HasCustomStructParamSupport)
        {
            genericTypes.AddRange(CustomStructParamTypes);
            genericConstraints.AddRange(CustomStructParamTypes.ConvertAll(paramType => $"MarshalledStruct<{paramType}>"));
        }

        if (genericTypes.Count > 0)
        {
            builder.AppendLine($"{protection}{returnType} {FunctionName}<{string.Join(", ", genericTypes)}>({ParamStringApiWithDefaults})");
            builder.Indent();
            foreach (var (genericType, constraint) in genericTypes.Zip(genericConstraints))
                builder.AppendLine($"where {genericType} : {constraint}");
            builder.UnIndent();
        }
        else
        {
            builder.AppendLine($"{protection}{returnType} {FunctionName}({ParamStringApiWithDefaults})");
        }
    }
    

    void ExportDeprecation(GeneratorStringBuilder builder)
    {
        if (Function.HasMetadata("DeprecatedFunction"))
        {
            string deprecationMessage = Function.GetMetadata("DeprecationMessage");
            if (deprecationMessage.Length == 0)
            {
                deprecationMessage = "This function is deprecated.";
            }
            else
            {
                // Remove nested quotes
                deprecationMessage = deprecationMessage.Replace("\"", "");
            }
            builder.AppendLine($"[Obsolete(\"{Function.SourceName} is deprecated: {deprecationMessage}\")]");
        }
    }

    void ExportSpecializationGetter(GeneratorStringBuilder builder)
    {
        if (HasCustomStructParamSupport)
        {
            int customStructureParamCount = CustomStructParamTypes.Count;
            string dictionaryKey = customStructureParamCount == 1
                ? "IntPtr"
                : $"({string.Join(", ", Enumerable.Repeat("IntPtr", customStructureParamCount))})";
            builder.AppendLine($"static Dictionary<{dictionaryKey}, IntPtr> {Function.SourceName}_Specializations = new Dictionary<{dictionaryKey}, IntPtr>();");
            builder.AppendLine($"static IntPtr {Function.SourceName}_GetSpecialization<{string.Join(", ", CustomStructParamTypes)}>()");
            builder.Indent();
            foreach (string customStructParamType in CustomStructParamTypes)
            {
                builder.AppendLine($"where {customStructParamType} : MarshalledStruct<{customStructParamType}>");
            }
            builder.UnIndent();
            builder.OpenBrace();
            builder.AppendLine("IntPtr specializationNativeFunction;");
            List<string> nativeClassPtrs = CustomStructParamTypes.ConvertAll(customStructParamType =>
                $"{customStructParamType}.GetNativeClassPtr()");
            string specializationKeyInitializer = nativeClassPtrs.Count == 1 ? nativeClassPtrs[0] : $"({string.Join(", ", nativeClassPtrs)})";
            builder.AppendLine($"{dictionaryKey} specializationKey = {specializationKeyInitializer};");
            builder.AppendLine($"if(!{Function.SourceName}_Specializations.TryGetValue(specializationKey, out specializationNativeFunction))");
            builder.OpenBrace();
            builder.BeginUnsafeBlock();
            string customStructBufferInitializationList = customStructureParamCount == 1 ? "specializationKey" : string.Join(", ", Enumerable.Range(1, customStructureParamCount).ToList().ConvertAll(i => $"specializationKey.Item{i}"));
            builder.AppendLine($"IntPtr* customStructBufferAllocation = stackalloc IntPtr[]{{{customStructBufferInitializationList}}};");
            builder.AppendLine("IntPtr customStructBuffer = (IntPtr) customStructBufferAllocation;");
            string nativeFunctionIntPtr = $"{Function.SourceName}_NativeFunction";
            string customStructNativePropertiesIntPtr = $"{Function.SourceName}_CustomStructureNativeProperties";
            builder.AppendLine($"fixed(nint* nativePropertyBuffer = {customStructNativePropertiesIntPtr})");
            builder.OpenBrace();
            builder.AppendLine($"specializationNativeFunction = {ExporterCallbacks.UFunctionCallbacks}.CallCreateNativeFunctionCustomStructSpecialization({nativeFunctionIntPtr}, (nint) nativePropertyBuffer, customStructBuffer);");
            builder.CloseBrace();
            builder.AppendLine($"{Function.SourceName}_Specializations.Add(specializationKey, specializationNativeFunction);");
            builder.EndUnsafeBlock();
            builder.CloseBrace();
            builder.AppendLine("return specializationNativeFunction;");
            builder.CloseBrace();
        }
    }

    void DetermineProtectionMode()
    {
        switch (ProtectionMode)
        {
            case EFunctionProtectionMode.UseUFunctionProtection:
                if (Function.HasAnyFlags(EFunctionFlags.Public | EFunctionFlags.BlueprintCallable))
                {
                    Modifiers = ScriptGeneratorUtilities.PublicKeyword;
                }
                else if (Function.HasAllFlags(EFunctionFlags.Protected))
                {
                    if (Function.HasMetadata("BlueprintProtected"))
                    {
                        Modifiers = ScriptGeneratorUtilities.ProtectedKeyword;
                    }
                    else
                    {
                        Modifiers = ScriptGeneratorUtilities.PublicKeyword;
                    }
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
        
        if (Function.HasAllFlags(EFunctionFlags.Static))
        {
            return invokeFunction + ".CallInvokeNativeStaticFunction";
        }
        
        if (Function.HasAllFlags(EFunctionFlags.Net))
        {
            return invokeFunction + ".CallInvokeNativeNetFunction";
        }
        
        if (Function.HasAllFlags(EFunctionFlags.HasOutParms) || Function.HasReturnProperty)
        {
            return invokeFunction + ".CallInvokeNativeFunctionOutParms";
        } 
        
        return invokeFunction + ".CallInvokeNativeFunction";
    }
    
    public string TryAddPrecedingCustomStructParams(UhtProperty parameter, string name)
    {
        if (!HasCustomStructParamSupport)
        {
            return name;
        }
        
        int precedingCustomStructParams = parameter.GetPrecedingCustomStructParams();
        if (precedingCustomStructParams > 0)
        {
            return name + $"<{string.Join(", ", CustomStructParamTypes.GetRange(0, precedingCustomStructParams))}>()";
        }
        
        return name;
    }
}

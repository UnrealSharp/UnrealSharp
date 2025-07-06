using System;
using System.Collections.Generic;
using System.Linq;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Exporters;
using UnrealSharpScriptGenerator.PropertyTranslators;

namespace UnrealSharpScriptGenerator.Utilities;

public class GetterSetterPair
{
    public GetterSetterPair(UhtProperty property)
    {
        PropertyName = property.GetPropertyName();

        if (!property.HasNativeGetter())
        {
            UhtFunction? foundGetter = property.GetBlueprintGetter();
            if (foundGetter != null)
            {
                Getter = foundGetter;
                GetterExporter = GetterSetterFunctionExporter.Create(foundGetter, property, GetterSetterMode.Get,
                    EFunctionProtectionMode.UseUFunctionProtection);
            }
        }

        if (!property.HasNativeSetter())
        {
            UhtFunction? foundSetter = property.GetBlueprintSetter();
            if (foundSetter != null)
            {
                Setter = foundSetter;
                SetterExporter = GetterSetterFunctionExporter.Create(foundSetter, property, GetterSetterMode.Set,
                    EFunctionProtectionMode.UseUFunctionProtection);
            }
        }
    }

    public GetterSetterPair(string propertyName)
    {
        PropertyName = propertyName;
    }

    public readonly string PropertyName;

    public UhtFunction? Getter { get; set; }
    public UhtFunction? Setter { get; set; }

    public GetterSetterFunctionExporter? GetterExporter { get; set; }
    public GetterSetterFunctionExporter? SetterExporter { get; set; }

    public List<UhtFunction> Accessors
    {
        get
        {
            List<UhtFunction> accessors = new();

            UhtFunction? getter = Getter;
            if (getter != null)
            {
                accessors.Add(getter);
            }

            UhtFunction? setter = Setter;
            if (setter != null)
            {
                accessors.Add(setter);
            }

            return accessors;
        }
    }

    public UhtProperty? Property { get; set; }
}

public static class ScriptGeneratorUtilities
{
    public const string InteropNamespace = "UnrealSharp.Interop";
    public const string MarshallerNamespace = "UnrealSharp.Core.Marshallers";
    public const string AttributeNamespace = "UnrealSharp.Attributes";
    public const string CoreAttributeNamespace = "UnrealSharp.Core.Attributes";
    public const string InteropServicesNamespace = "System.Runtime.InteropServices";
    
    public const string PublicKeyword = "public ";
    public const string PrivateKeyword = "private ";
    public const string ProtectedKeyword = "protected ";
    
    public const string IntPtrZero = "IntPtr.Zero";

    public static string TryGetPluginDefine(string key)
    {
        Program.PluginModule.TryGetDefine(key, out string? generatedCodePath);
        return generatedCodePath!;
    }

    public static bool CanExportFunction(UhtFunction function)
    {
        if (function.HasAnyFlags(EFunctionFlags.Delegate | EFunctionFlags.MulticastDelegate))
        {
            return false;
        }

        return CanExportParameters(function);
    }

    public static bool CanExportParameters(UhtFunction function)
    {
        bool CanExportParameter(UhtProperty property, Func<PropertyTranslator, bool> isSupported)
        {
            PropertyTranslator? translator = PropertyTranslatorManager.GetTranslator(property);
            return translator != null && isSupported(translator) && translator.CanExport(property);
        }

        if (function.ReturnProperty != null && !CanExportParameter(function.ReturnProperty,
                translator => translator.IsSupportedAsReturnValue()))
        {
            return false;
        }

        foreach (UhtProperty parameter in function.Properties)
        {
            if (!CanExportParameter(parameter, translator => translator.IsSupportedAsParameter()))
            {
                return false;
            }
        }

        return true;
    }

    public static bool CanExportProperty(UhtProperty property)
    {
        PropertyTranslator? translator = PropertyTranslatorManager.GetTranslator(property);
        if (translator == null || !translator.CanExport(property))
        {
            return false;
        }

        bool isClassProperty = property.Outer!.EngineType == UhtEngineType.Class;
        bool canBeClassProperty = isClassProperty && translator.IsSupportedAsProperty();
        bool canBeStructProperty = !isClassProperty && translator.IsSupportedAsStructProperty();
        return canBeClassProperty || canBeStructProperty;
    }

    public static string GetCleanEnumValueName(UhtEnum enumObj, UhtEnumValue enumValue)
    {
        if (enumObj.CppForm == UhtEnumCppForm.Regular)
        {
            return enumValue.Name;
        }

        int delimiterIndex = enumValue.Name.IndexOf("::", StringComparison.Ordinal);
        return delimiterIndex < 0 ? enumValue.Name : enumValue.Name.Substring(delimiterIndex + 2);
    }

    public static void GetExportedProperties(UhtStruct structObj, List<UhtProperty> properties,
        Dictionary<UhtProperty, GetterSetterPair> getterSetterBackedProperties)
    {
        if (!structObj.Properties.Any())
        {
            return;
        }

        UhtClass? classObj = structObj as UhtClass;
        foreach (UhtProperty property in structObj.Properties)
        {
            if (!CanExportProperty(property) || InclusionLists.HasBannedProperty(property))
            {
                continue;
            }

            if (classObj != null && (property.HasAnyGetter() || property.HasAnySetter()))
            {
                GetterSetterPair pair = new GetterSetterPair(property);
                getterSetterBackedProperties.Add(property, pair);
            }
            else
            {
                properties.Add(property);
            }
        }
    }

    public static void GetExportedFunctions(UhtClass classObj, List<UhtFunction> functions,
        List<UhtFunction> overridableFunctions, Dictionary<string, GetterSetterPair> getterSetterPairs)
    {
        List<UhtFunction> exportedFunctions = new();

        bool HasFunction(List<UhtFunction> functionsToCheck, UhtFunction functionToTest)
        {
            foreach (UhtFunction function in functionsToCheck)
            {
                if (function.SourceName == functionToTest.SourceName ||
                    function.CppImplName == functionToTest.CppImplName)
                {
                    return true;
                }
            }

            return false;
        }

        foreach (UhtFunction function in classObj.Functions)
        {
            if (!CanExportFunction(function))
            {
                continue;
            }

            if (function.IsAnyGetter() || function.IsAnySetter())
            {
                continue;
            }

            if (function.FunctionFlags.HasAnyFlags(EFunctionFlags.BlueprintEvent))
            {
                overridableFunctions.Add(function);
            }
            else if (function.IsAutocast())
            {
                functions.Add(function);

                if (function.Properties.First() is not UhtStructProperty structToConvertProperty)
                {
                    continue;
                }

                if (structToConvertProperty.Package.IsPartOfEngine() != function.Package.IsPartOfEngine())
                {
                    // For auto-casts to work, they both need to be in the same generated assembly. 
                    // Currently not supported, as we separate engine and project generated assemblies.
                    continue;
                }

                AutocastExporter.AddAutocastFunction(structToConvertProperty.ScriptStruct, function);
            }
            else if (!TryMakeFunctionGetterSetterPair(function, classObj, getterSetterPairs))
            {
                functions.Add(function);
            }

            exportedFunctions.Add(function);
        }

        foreach (UhtClass declaration in classObj.GetInterfaces())
        {
            UhtClass? interfaceClass = declaration.GetInterfaceAlternateClass();

            if (interfaceClass == null)
            {
                continue;
            }

            foreach (UhtFunction function in interfaceClass.Functions)
            {
                if (HasFunction(exportedFunctions, function) || !CanExportFunction(function))
                {
                    continue;
                }

                if (function.FunctionFlags.HasAnyFlags(EFunctionFlags.BlueprintEvent))
                {
                    overridableFunctions.Add(function);
                }
                else
                {
                    functions.Add(function);
                }
            }
        }
    }

    public static List<UhtClass> GetInterfaces(this UhtClass classObj)
    {
        List<UhtClass> interfaces = new();
        foreach (UhtStruct interfaceClass in classObj.Bases)
        {
            UhtEngineType engineType = interfaceClass.EngineType;
            if (engineType is UhtEngineType.Interface or UhtEngineType.NativeInterface)
            {
                interfaces.Add((UhtClass)interfaceClass);
            }
        }

        return interfaces;
    }

    public static bool TryMakeFunctionGetterSetterPair(UhtFunction function, UhtClass classObj,
        Dictionary<string, GetterSetterPair> getterSetterPairs)
    {
        string scriptName = function.GetFunctionName();
        bool isGetter = CheckIfGetter(scriptName, function);
        bool isSetter = CheckIfSetter(scriptName, function);

        if (!isGetter && !isSetter)
        {
            return false;
        }

        string propertyName = scriptName.Length > 3 ? scriptName.Substring(3) : function.SourceName;
        propertyName = NameMapper.EscapeKeywords(propertyName);

        UhtFunction? sameNameFunction = classObj.FindFunctionByName(propertyName);

        if (sameNameFunction != null && sameNameFunction != function)
        {
            return false;
        }

        bool ComparePropertyName(UhtProperty prop, string name)
        {
            return prop.SourceName == name || prop.GetPropertyName() == name;
        }

        UhtProperty? classProperty = classObj.FindPropertyByName(propertyName, ComparePropertyName);
        UhtProperty firstProperty = function.ReturnProperty ?? function.Properties.First();

        if (classProperty != null && (!classProperty.IsSameType(firstProperty) || classProperty.HasAnyGetter() ||
                                      classProperty.HasAnySetter()))
        {
            return false;
        }

        if (!getterSetterPairs.TryGetValue(propertyName, out GetterSetterPair? pair))
        {
            pair = new GetterSetterPair(propertyName);
            getterSetterPairs[propertyName] = pair;
        }

        if (pair.Accessors.Count == 2)
        {
            return true;
        }

        bool isOutParm = function.Properties.Any(p =>
            p.HasAllFlags(EPropertyFlags.OutParm) && !p.HasAllFlags(EPropertyFlags.ConstParm));

        if (function.ReturnProperty != null || isOutParm)
        {
            pair.Getter = function;
            pair.GetterExporter = GetterSetterFunctionExporter.Create(function, firstProperty, GetterSetterMode.Get,
                EFunctionProtectionMode.UseUFunctionProtection);

            UhtFunction? setter = classObj.FindFunctionByName("Set" + propertyName, null, true);
            if (setter != null && CheckIfSetter(setter))
            {
                pair.Setter = setter;
                pair.SetterExporter = GetterSetterFunctionExporter.Create(setter, firstProperty, GetterSetterMode.Set,
                    EFunctionProtectionMode.UseUFunctionProtection);
            }
        }
        else
        {
            pair.Setter = function;
            pair.SetterExporter = GetterSetterFunctionExporter.Create(function, firstProperty, GetterSetterMode.Set,
                EFunctionProtectionMode.UseUFunctionProtection);

            UhtFunction? getter = classObj.FindFunctionByName("Get" + propertyName, null, true);
            if (getter != null && CheckIfGetter(getter))
            {
                pair.Getter = getter;
                pair.GetterExporter = GetterSetterFunctionExporter.Create(getter, firstProperty, GetterSetterMode.Get,
                    EFunctionProtectionMode.UseUFunctionProtection);
            }
        }

        pair.Property = firstProperty;
        getterSetterPairs[propertyName] = pair;
        return true;
    }

    static bool CheckIfGetter(string scriptName, UhtFunction function)
    {
        return scriptName.StartsWith("Get") && CheckIfGetter(function);
    }

    static bool CheckIfGetter(UhtFunction function)
    {
        int childrenCount = function.Children.Count;
        bool hasReturnProperty = function.ReturnProperty != null;
        bool hasNoParameters = !function.HasParameters;
        bool hasSingleOutParam = !hasNoParameters && childrenCount == 1 && function.HasOutParams();
        bool hasWorldContextPassParam =
            childrenCount == 2 && function.Properties.Any(property => property.IsWorldContextParameter());
        return hasReturnProperty && (hasNoParameters || hasSingleOutParam || hasWorldContextPassParam);
    }

    static bool CheckIfSetter(string scriptName, UhtFunction function)
    {
        return scriptName.StartsWith("Set") && CheckIfSetter(function);
    }

    static bool CheckIfSetter(UhtFunction function)
    {
        bool hasSingleParameter = function.Properties.Count() == 1;
        bool isNotOutOrReferenceParam = function.HasParameters && !function.Properties.First()
            .HasAllFlags(EPropertyFlags.OutParm | EPropertyFlags.ReferenceParm);
        return hasSingleParameter && isNotOutOrReferenceParam;
    }
}
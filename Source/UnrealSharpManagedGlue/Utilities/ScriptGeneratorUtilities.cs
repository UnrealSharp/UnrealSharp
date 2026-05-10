using System;
using System.Collections.Generic;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.Exporters;

namespace UnrealSharpManagedGlue.Utilities;

public class GetterSetterPair
{
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

    public readonly UhtProperty Property;
    
    public GetterSetterPair(UhtProperty property)
    {
        PropertyName = property.GetPropertyName();

        if (!property.HasNativeGetter())
        {
            UhtFunction? foundGetter = property.GetBlueprintGetter();
            if (foundGetter != null)
            {
                Getter = foundGetter;
                GetterExporter = GetterSetterFunctionExporter.Create(foundGetter, property, GetterSetterMode.Get, EFunctionProtectionMode.UseUFunctionProtection);
            }
        }

        if (!property.HasNativeSetter())
        {
            UhtFunction? foundSetter = property.GetBlueprintSetter();
            if (foundSetter != null)
            {
                Setter = foundSetter;
                SetterExporter = GetterSetterFunctionExporter.Create(foundSetter, property, GetterSetterMode.Set, EFunctionProtectionMode.UseUFunctionProtection);
            }
        }
        
        Property = property;
    }

    public GetterSetterPair(string propertyName, UhtProperty primaryProperty)
    {
        PropertyName = propertyName;
        Property = primaryProperty;
    }
}

public static class ScriptGeneratorUtilities
{
    public const string InteropNamespace = "UnrealSharp.Interop";
    public const string CoreNamespace = "UnrealSharp.Core";
    public const string MarshallerNamespace = "UnrealSharp.Core.Marshallers";
    public const string AttributeNamespace = "UnrealSharp.Attributes";
    public const string CoreAttributeNamespace = "UnrealSharp.Core.Attributes";
    public const string InteropServicesNamespace = "System.Runtime.InteropServices";
    
    public const string PublicKeyword = "public ";
    public const string PrivateKeyword = "private ";
    public const string ProtectedKeyword = "protected ";
    
    public const string IntPtrZero = "IntPtr.Zero";

    public static string TryGetPluginStringDefine(string key)
    {
        GeneratorStatics.PluginModule.TryGetDefine(key, out string? generatedCodePath);
        return generatedCodePath!;
    }
    
    public static int TryGetPluginIntDefine(string key)
    {
        GeneratorStatics.PluginModule.TryGetDefine(key, out int valueStr);
        return valueStr;
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
    
}
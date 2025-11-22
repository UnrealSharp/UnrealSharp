using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using UnrealSharp.GlueGenerator.NativeTypes;

namespace UnrealSharp.GlueGenerator;

public static class SourceGenUtilities
{
    public const string ParamsBuffer = "paramsAlloc";
    public const string ParamsBufferAllocation = "alloc";
    public const string IntPtrZero = "IntPtr.Zero";
    
    public const string Buffer = "buffer";
    
    public const string NativeTypePtr = "NativeTypePtr";
    public const string NativeObject = "NativeObject";
    
    public const string ReturnAssignment = "return ";
    public const string ValueParam = "value";
    
    public const string ReturnValueName = "ReturnValue";
    
    public static bool HasAttribute(this ISymbol symbol, string attributeFullName)
    {
        foreach (AttributeData attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass is null)
            {
                continue;
            }
            
            if (attribute.AttributeClass.Name == attributeFullName)
            {
                return true;
            }
        }

        return false;
    }
    
    public static bool HasUFunctionAttribute(this ISymbol symbol)
    {
        return HasAttribute(symbol, "UFunctionAttribute");
    }
    
    public static string TryGetEngineName(this ISymbol symbol)
    {
        foreach (AttributeData attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass!.Name != "GeneratedTypeAttribute")
            {
                continue;
            }

            return (string) attribute.ConstructorArguments[0].Value!;
        }
        
        return string.Empty;
    }
    
    public static List<AttributeData> GetAttributesByName(this ISymbol symbol, string attributeFullName)
    {
        ImmutableArray<AttributeData> symbolAttributes = symbol.GetAttributes();
        int attributeCount = symbolAttributes.Length;
        
        List<AttributeData> attributes = new List<AttributeData>(attributeCount);
        
        for (int i = 0; i < attributeCount; i++)
        {
            AttributeData attribute = symbolAttributes[i];
            if (attribute.AttributeClass!.Name == attributeFullName)
            {
                attributes.Add(attribute);
            }
        }

        return attributes;
    }
    
    public static List<MetaDataInfo> GetUMetaAttributes(this ISymbol symbol)
    {
        List<MetaDataInfo> attributes = new List<MetaDataInfo>(symbol.GetAttributes().Length);
        
        foreach (AttributeData attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass!.Name == "UMetaDataAttribute")
            {
                string key = string.Empty;
                if (attribute.ConstructorArguments[0].Value is string keyArg)
                {
                    key = keyArg;
                }
                
                string value = string.Empty;
                if (attribute.ConstructorArguments[1].Value is string valueArg)
                {
                    value = valueArg;
                }
                
                attributes.Add(new MetaDataInfo(key, value));
                continue;
            }

            if (attribute.AttributeClass.ContainingNamespace.Name == "MetaTags")
            {
                string attributeName = attribute.AttributeClass.Name;
                int index = attributeName.IndexOf("Attribute", StringComparison.OrdinalIgnoreCase);
                string name = attributeName.Substring(0, index);
                
                string value = string.Empty;
                if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is string valueArg2)
                {
                    value = valueArg2;
                }
                
                attributes.Add(new MetaDataInfo(name, value));
            }
        }

        return attributes;
    }
    
    public static string RefKindToString(this RefKind refKind)
    {
        return refKind switch
        {
            RefKind.None => string.Empty,
            RefKind.Ref => "ref ",
            RefKind.Out => "out ",
            RefKind.In => "in ",
            _ => throw new ArgumentOutOfRangeException(nameof(refKind), refKind, null)
        };
    }
    
    public static string AccessibilityToString(this Accessibility accessibility)
    {
        return accessibility switch
        {
            Accessibility.Public => "public ",
            Accessibility.Private => "private ",
            Accessibility.Protected => "protected ",
            Accessibility.Internal => "internal ",
            Accessibility.ProtectedOrInternal => "protected internal ",
            Accessibility.ProtectedAndInternal => "private protected ",
            _ => string.Empty
        };
    }
    
    public static string GetEnumNameFromValue(ITypeSymbol enumType, byte value)
    {
        foreach (ISymbol? member in enumType.GetMembers())
        {
            if (member is not IFieldSymbol field || field.ConstantValue is null || (byte) field.ConstantValue != value)
            {
                continue;
            }

            return field.Name;
        }

        return value.ToString();
    }
}
﻿using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class ObjectPropertyTranslator : SimpleTypePropertyTranslator
{
    public ObjectPropertyTranslator() : base(typeof(UhtObjectProperty))
    {
        
    }

    public override bool CanExport(UhtProperty property)
    {
        UhtObjectPropertyBase objectProperty = (UhtObjectPropertyBase) property;
        UhtClass? metaClass = objectProperty.Class;
        
        if (metaClass.HasAnyFlags(EClassFlags.Interface) ||
            metaClass.EngineType == UhtEngineType.Interface || 
            metaClass.EngineType == UhtEngineType.NativeInterface)
        {
            return false;
        }
        
        return base.CanExport(property);
    }

    public override string GetNullValue(UhtProperty property)
    {
        return "null";
    }

    public override string GetManagedType(UhtProperty property)
    {
        return GetManagedType(property, property.HasMetadata("Nullable"));   
    }

    private static string GetManagedType(UhtProperty property, bool isNullable)
    {
        string nullableAnnotation = isNullable ? "?" : "";
        if (property.IsGenericType()) return $"DOT{nullableAnnotation}";

        UhtObjectProperty objectProperty = (UhtObjectProperty)property;
        return $"{objectProperty.Class.GetFullManagedName()}{nullableAnnotation}";
    }

    public override string GetMarshaller(UhtProperty property)
    {
        if (property.Outer is UhtProperty outerProperty && outerProperty.IsGenericType())
        {
            return "ObjectMarshaller<DOT>";
        }

        return $"ObjectMarshaller<{GetManagedType(property, false)}>";
    }

    public override bool CanSupportGenericType(UhtProperty property) => true;
}
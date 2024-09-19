using System;
using System.Collections.Generic;
using EpicGames.Core;
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

    public override void GetReferences(UhtProperty property, List<UhtType> references)
    {
        base.GetReferences(property, references);
        UhtObjectPropertyBase objectProperty = (UhtObjectPropertyBase)property;
        references.Add(objectProperty.Class);
    }

    public override string GetManagedType(UhtProperty property)
    {
        UhtObjectProperty objectProperty = (UhtObjectProperty)property;
        return objectProperty.Class.GetFullManagedName();
    }

    public override string GetMarshaller(UhtProperty property)
    {
        return $"ObjectMarshaller<{GetManagedType(property)}>";
    }
}
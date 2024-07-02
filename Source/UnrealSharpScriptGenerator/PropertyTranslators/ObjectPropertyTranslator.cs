using System;
using System.Collections.Generic;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class ObjectPropertyTranslator : SimpleTypePropertyTranslator
{
    public ObjectPropertyTranslator() : base(typeof(UhtObjectProperty))
    {
        
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
        return ScriptGeneratorUtilities.GetFullManagedName(objectProperty.Class);
    }

    public override string GetMarshaller(UhtProperty property)
    {
        return $"ObjectMarshaller<{GetManagedType(property)}>";
    }
}
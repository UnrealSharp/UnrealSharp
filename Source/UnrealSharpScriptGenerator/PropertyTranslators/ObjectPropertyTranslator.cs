using System;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class ObjectPropertyTranslator : SimpleTypePropertyTranslator
{
    public ObjectPropertyTranslator() : base(typeof(UhtObjectProperty))
    {
        
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
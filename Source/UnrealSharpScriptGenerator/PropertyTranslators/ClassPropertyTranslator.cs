using System;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class ClassPropertyTranslator : SimpleTypePropertyTranslator
{
    public ClassPropertyTranslator() : base(typeof(UhtClassProperty))
    {
    }

    public override string GetManagedType(UhtProperty property)
    {
        UhtClassProperty classProperty = (UhtClassProperty) property;
        string fullName = classProperty.MetaClass!.GetFullManagedName();
        return $"TSubclassOf<{fullName}>";
    }

    public override string GetMarshaller(UhtProperty property)
    {
        UhtClassProperty classProperty = (UhtClassProperty)property;
        string fullName = classProperty.MetaClass!.GetFullManagedName();
        return $"SubclassOfMarshaller<{fullName}>";
    }
}
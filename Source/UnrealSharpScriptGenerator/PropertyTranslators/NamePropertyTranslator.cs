using System;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class NamePropertyTranslator : BlittableTypePropertyTranslator
{
    public NamePropertyTranslator() : base(typeof(UhtNameProperty), "Name")
    {
    }
    
    public override bool ExportDefaultParameter => false;

    public override string GetNullValue(UhtProperty property)
    {
        return "Name.None";
    }

    public override string ExportMarshallerDelegates(UhtProperty property)
    {
        return base.ExportMarshallerDelegates(property);
    }
}
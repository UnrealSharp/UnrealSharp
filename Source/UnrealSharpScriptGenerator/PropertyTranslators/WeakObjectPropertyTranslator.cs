using System;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class WeakObjectPropertyTranslator : BlittableTypePropertyTranslator
{
    public WeakObjectPropertyTranslator() : base(typeof(UhtWeakObjectPtrProperty), "WeakObject")
    {
    }

    public override string GetManagedType(UhtProperty property)
    {
        UhtWeakObjectPtrProperty weakObjectProperty = (UhtWeakObjectPtrProperty)property;
        string fullName = ScriptGeneratorUtilities.GetFullManagedName(weakObjectProperty.Class);
        return $"WeakObject<{fullName}>";
    }

    public override bool CanExport(UhtProperty property)
    {
        return property is UhtWeakObjectPtrProperty;
    }
}
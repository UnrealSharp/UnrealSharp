using System;
using System.Collections.Generic;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class WeakObjectPropertyTranslator : BlittableTypePropertyTranslator
{
    public WeakObjectPropertyTranslator() : base(typeof(UhtWeakObjectPtrProperty), "WeakObject")
    {
    }
    
    public override bool ExportDefaultParameter => false;

    public override string GetManagedType(UhtProperty property)
    {
        UhtWeakObjectPtrProperty weakObjectProperty = (UhtWeakObjectPtrProperty)property;
        string fullName = weakObjectProperty.Class.GetFullManagedName();
        return $"TWeakObjectPtr<{fullName}>";
    }

    public override void GetReferences(UhtProperty property, List<UhtType> references)
    {
        base.GetReferences(property, references);
        UhtWeakObjectPtrProperty weakObjectProperty = (UhtWeakObjectPtrProperty)property;
        references.Add(weakObjectProperty.Class);
    }

    public override bool CanExport(UhtProperty property)
    {
        return property is UhtWeakObjectPtrProperty;
    }
}
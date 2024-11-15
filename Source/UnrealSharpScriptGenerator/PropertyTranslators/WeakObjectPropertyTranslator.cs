using System;
using System.Collections.Generic;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class WeakObjectPropertyTranslator : BlittableTypePropertyTranslator
{
    public WeakObjectPropertyTranslator() : base(typeof(UhtWeakObjectPtrProperty), "TWeakObjectPtr")
    {
    }
    
    public override bool ExportDefaultParameter => false;

    public override string GetManagedType(UhtProperty property)
    {
        UhtWeakObjectPtrProperty weakObjectProperty = (UhtWeakObjectPtrProperty)property;
        string fullName = weakObjectProperty.Class.GetFullManagedName();
        return $"TWeakObjectPtr<{fullName}>";
    }

    public override bool CanExport(UhtProperty property)
    {
        return property is UhtWeakObjectPtrProperty;
    }
}
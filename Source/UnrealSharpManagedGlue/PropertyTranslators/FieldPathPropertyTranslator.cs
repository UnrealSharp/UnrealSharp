using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class FieldPathPropertyTranslator : SimpleTypePropertyTranslator
{
    public FieldPathPropertyTranslator() : base(typeof(UhtFieldPathProperty))
    {
    }

    public override bool CanExport(UhtProperty property)
    {
        return property is UhtFieldPathProperty;
    }

    public override string GetManagedType(UhtProperty property)
    {
        return "UnrealSharp.CoreUObject.FFieldPath";
    }

    public override string GetMarshaller(UhtProperty property)
    {
        return "UnrealSharp.CoreUObject.FieldPathMarshaller";
    }

    public override bool CanSupportGenericType(UhtProperty property)
    {
        return false;
    }
}
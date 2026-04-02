using EpicGames.UHT.Types;

namespace UnrealSharpManagedGlue.PropertyTranslators;

public class FieldPathPropertyTranslator : SimpleTypePropertyTranslator
{
    public FieldPathPropertyTranslator() : base(typeof(UhtFieldPathProperty), "UnrealSharp.CoreUObject.FFieldPath", "UnrealSharp.CoreUObject.FieldPathMarshaller")
    {
    }

    public override bool CanSupportGenericType(UhtProperty property)
    {
        return false;
    }
}
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

    public override string GetManagedType(UhtProperty property)
    {
        if (property.HasMetaData("GenericType"))
            return property.GetMetaData("GenericType");

        UhtObjectProperty objectProperty = (UhtObjectProperty)property;
        return objectProperty.Class.GetFullManagedName();
    }

    public override string GetMarshaller(UhtProperty property)
    {
        if (property.Outer != null 
            && property.Outer.HasMetadata("GenericType"))
        {
            return $"ObjectMarshaller<{property.Outer.GetMetadata("GenericType")}>";
        }

        return $"ObjectMarshaller<{GetManagedType(property)}>";
    }

    public override bool CanSupportGenericType(UhtProperty property) => true;
}
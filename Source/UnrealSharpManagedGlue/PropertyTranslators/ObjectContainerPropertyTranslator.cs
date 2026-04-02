using System;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue.PropertyTranslators;

public abstract class ObjectContainerPropertyTranslator : SimpleTypePropertyTranslator
{
    private readonly string _marshaller;
    
    public ObjectContainerPropertyTranslator(Type propertyType, string managedType, string marshaller) : base(propertyType, managedType)
    {
        _marshaller = marshaller;
    }
    
    protected abstract UhtClass GetMetaClass(UhtObjectPropertyBase property);
    
    public override string GetManagedType(UhtProperty property)
    {
        UhtObjectPropertyBase objectContainerProperty = (UhtObjectPropertyBase) property;
        return $"{ManagedType}<{GetFullName(objectContainerProperty)}>";
    }

    public override string GetMarshaller(UhtProperty property)
    {
        UhtObjectPropertyBase objectContainerProperty = (UhtObjectPropertyBase) property;
        return $"{_marshaller}<{GetFullName(objectContainerProperty)}>";
    }
    
    string GetFullName(UhtObjectPropertyBase property)
    {
        UhtClass metaClass = GetMetaClass(property);
        return property.IsGenericType() ? "DOT" : metaClass.GetFullManagedName();
    }
}
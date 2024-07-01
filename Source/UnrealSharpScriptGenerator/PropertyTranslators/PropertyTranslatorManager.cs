using System;
using System.Collections.Generic;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public static class PropertyTranslatorManager
{
    private static readonly Dictionary<Type, List<PropertyTranslator>> RegisteredTranslators = new();
    
    static PropertyTranslatorManager()
    {
        AddBlittablePropertyTranslator(typeof(UhtInt8Property), "sbyte");
        AddBlittablePropertyTranslator(typeof(UhtInt16Property), "short");
        AddBlittablePropertyTranslator(typeof(UhtIntProperty), "int");
        AddBlittablePropertyTranslator(typeof(UhtInt64Property), "long");
        AddBlittablePropertyTranslator(typeof(UhtUInt16Property), "ushort");
        AddBlittablePropertyTranslator(typeof(UhtUInt32Property), "uint");
        AddBlittablePropertyTranslator(typeof(UhtUInt64Property), "ulong");
        AddBlittablePropertyTranslator(typeof(UhtDoubleProperty), "double");
        AddBlittablePropertyTranslator(typeof(UhtByteProperty), "byte");
        AddPropertyTranslator(typeof(UhtFloatProperty), new FloatPropertyTranslator());
        
        EnumPropertyHandler enumPropertyHandler = new();
        AddPropertyTranslator(typeof(UhtEnumProperty), enumPropertyHandler);
        AddPropertyTranslator(typeof(UhtByteProperty), enumPropertyHandler);
        
        AddBlittablePropertyTranslator(typeof(UhtByteProperty), "byte");
        
        AddPropertyTranslator(typeof(UhtBoolProperty), new BoolPropertyTranslator());
        AddPropertyTranslator(typeof(StringPropertyTranslator), new StringPropertyTranslator());
        AddPropertyTranslator(typeof(NamePropertyTranslator), new NamePropertyTranslator());
        AddPropertyTranslator(typeof(TextPropertyTranslator), new TextPropertyTranslator());
        
        AddPropertyTranslator(typeof(UhtWeakObjectPtrProperty), new WeakObjectPropertyTranslator());
        AddPropertyTranslator(typeof(UhtObjectProperty), new ObjectPropertyTranslator());
        AddPropertyTranslator(typeof(UhtClassProperty), new ClassPropertyTranslator());
        AddPropertyTranslator(typeof(UhtSoftClassProperty), new SoftClassPropertyTranslator());
        AddPropertyTranslator(typeof(UhtSoftObjectProperty), new SoftObjectPropertyTranslator());
        AddPropertyTranslator(typeof(UhtSoftClassProperty), new SoftClassPropertyTranslator());
        
        AddBlittableCustomStructPropertyTranslator("Vector2f", "System.Numerics.Vector2");
        AddBlittableCustomStructPropertyTranslator("Vector3f", "System.Numerics.Vector3");
        AddBlittableCustomStructPropertyTranslator("Vector_NetQuantize", "UnrealSharp.CoreUObject.Vector");
        AddBlittableCustomStructPropertyTranslator("Vector_NetQuantize10", "UnrealSharp.CoreUObject.Vector");
        AddBlittableCustomStructPropertyTranslator("Vector_NetQuantize100", "UnrealSharp.CoreUObject.Vector");
        AddBlittableCustomStructPropertyTranslator("Vector_NetQuantizeNormal", "UnrealSharp.CoreUObject.Vector");
        AddBlittableCustomStructPropertyTranslator("Vector4f", "System.Numerics.Vector4");
        AddBlittableCustomStructPropertyTranslator("Quatf4", "System.Numerics.Quaternion");
        AddBlittableCustomStructPropertyTranslator("Matrix44f", "System.Numerics.Matrix4x4");
        
        AddBlittableCustomStructPropertyTranslator("TimerHandle", "UnrealSharp.Engine.TimerHandle");
        AddBlittableCustomStructPropertyTranslator("InputActionValue", "UnrealSharp.EnhancedInput.InputActionValue");
        
        AddPropertyTranslator(typeof(UhtStructProperty), new BlittableStructPropertyTranslator());
        AddPropertyTranslator(typeof(UhtStructProperty), new StructPropertyTranslator());
        
    }
    
    public static PropertyTranslator? GetTranslator(UhtProperty property)
    {
        if (!RegisteredTranslators.TryGetValue(property.GetType(), out var translator))
        {
            return null;
        }
        
        foreach (PropertyTranslator propertyTranslator in translator)
        {
            if (propertyTranslator.CanExport(property))
            {
                return propertyTranslator;
            }
        }
        
        return null;
    }
    
    public static void AddBlittablePropertyTranslator(Type propertyType, string managedType)
    {
        List<PropertyTranslator> translators;
        if (RegisteredTranslators.TryGetValue(propertyType, out translators))
        {
            translators.Add(new BlittableTypePropertyTranslator(propertyType, managedType));
            return;
        }
        
        RegisteredTranslators.Add(propertyType, new List<PropertyTranslator> {new BlittableTypePropertyTranslator(propertyType, managedType)});
    }

    private static void AddPropertyTranslator(Type propertyClass, PropertyTranslator? translator)
    {
        if (RegisteredTranslators.TryGetValue(propertyClass, out List<PropertyTranslator> translators))
        {
            translators.Add(translator);
            return;
        }
        
        RegisteredTranslators.Add(propertyClass, new List<PropertyTranslator> {translator});
    }
    
    private static void AddBlittableCustomStructPropertyTranslator(string nativeName, string managedType)
    {
        AddPropertyTranslator(typeof(UhtStructProperty), new BlittableCustomStructTypePropertyTranslator(nativeName, managedType));
    }
    
}
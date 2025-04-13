using System;
using System.Collections.Generic;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public static class PropertyTranslatorManager
{
    private static readonly Dictionary<Type, List<PropertyTranslator>?> RegisteredTranslators = new();
    public static readonly List<string> ManuallyExportedTypes = new();
    
    static PropertyTranslatorManager()
    {
        ManuallyExportedTypes.Add("EStreamingSourcePriority");
        ManuallyExportedTypes.Add("ETriggerEvent");

        EnumPropertyTranslator enumPropertyTranslator = new();
        AddPropertyTranslator(typeof(UhtEnumProperty), enumPropertyTranslator);
        AddPropertyTranslator(typeof(UhtByteProperty), enumPropertyTranslator);
        
        AddBlittablePropertyTranslator(typeof(UhtInt8Property), "sbyte");
        AddBlittablePropertyTranslator(typeof(UhtInt16Property), "short");
        AddBlittablePropertyTranslator(typeof(UhtInt64Property), "long");
        AddBlittablePropertyTranslator(typeof(UhtUInt16Property), "ushort");
        AddBlittablePropertyTranslator(typeof(UhtUInt32Property), "uint");
        AddBlittablePropertyTranslator(typeof(UhtUInt64Property), "ulong");
        AddBlittablePropertyTranslator(typeof(UhtDoubleProperty), "double");
        AddBlittablePropertyTranslator(typeof(UhtByteProperty), "byte");
        AddBlittablePropertyTranslator(typeof(UhtLargeWorldCoordinatesRealProperty), "double");
        AddPropertyTranslator(typeof(UhtFloatProperty), new FloatPropertyTranslator());
        AddPropertyTranslator(typeof(UhtIntProperty), new IntPropertyTranslator());

        MulticastDelegatePropertyTranslator multicastDelegatePropertyTranslator = new();
        AddPropertyTranslator(typeof(UhtMulticastSparseDelegateProperty), multicastDelegatePropertyTranslator);
        AddPropertyTranslator(typeof(UhtMulticastDelegateProperty), multicastDelegatePropertyTranslator);
        AddPropertyTranslator(typeof(UhtMulticastInlineDelegateProperty), multicastDelegatePropertyTranslator);
        AddPropertyTranslator(typeof(UhtDelegateProperty), new SinglecastDelegatePropertyTranslator());
        
        AddBlittablePropertyTranslator(typeof(UhtByteProperty), "byte");
        
        AddPropertyTranslator(typeof(UhtBoolProperty), new BoolPropertyTranslator());
        AddPropertyTranslator(typeof(UhtStrProperty), new StringPropertyTranslator());
        AddPropertyTranslator(typeof(UhtNameProperty), new NamePropertyTranslator());
        AddPropertyTranslator(typeof(UhtTextProperty), new TextPropertyTranslator());
        
        AddPropertyTranslator(typeof(UhtWeakObjectPtrProperty), new WeakObjectPropertyTranslator());
        
        WorldContextObjectPropertyTranslator worldContextObjectPropertyTranslator = new();
        AddPropertyTranslator(typeof(UhtObjectPropertyBase), worldContextObjectPropertyTranslator);
        AddPropertyTranslator(typeof(UhtObjectPtrProperty), worldContextObjectPropertyTranslator);
        AddPropertyTranslator(typeof(UhtObjectProperty), worldContextObjectPropertyTranslator);
        AddPropertyTranslator(typeof(UhtLazyObjectPtrProperty), worldContextObjectPropertyTranslator);
        
        ObjectPropertyTranslator objectPropertyTranslator = new();
        AddPropertyTranslator(typeof(UhtObjectPropertyBase), objectPropertyTranslator);
        AddPropertyTranslator(typeof(UhtObjectPtrProperty), objectPropertyTranslator);
        AddPropertyTranslator(typeof(UhtObjectProperty), objectPropertyTranslator);
        AddPropertyTranslator(typeof(UhtLazyObjectPtrProperty), objectPropertyTranslator);
        
        AddPropertyTranslator(typeof(UhtInterfaceProperty), new InterfacePropertyTranslator());
        
        AddPropertyTranslator(typeof(UhtClassProperty), new ClassPropertyTranslator());
        AddPropertyTranslator(typeof(UhtClassPtrProperty), new ClassPropertyTranslator());
        AddPropertyTranslator(typeof(UhtSoftClassProperty), new SoftClassPropertyTranslator());
        AddPropertyTranslator(typeof(UhtSoftObjectProperty), new SoftObjectPropertyTranslator());
        
        AddBlittableCustomStructPropertyTranslator("FVector2f", "UnrealSharp.CoreUObject.FVector2f");
        AddBlittableCustomStructPropertyTranslator("FVector3f", "UnrealSharp.CoreUObject.FVector3f");
        AddBlittableCustomStructPropertyTranslator("FVector_NetQuantize", "UnrealSharp.CoreUObject.FVector");
        AddBlittableCustomStructPropertyTranslator("FVector_NetQuantize10", "UnrealSharp.CoreUObject.FVector");
        AddBlittableCustomStructPropertyTranslator("FVector_NetQuantize100", "UnrealSharp.CoreUObject.FVector");
        AddBlittableCustomStructPropertyTranslator("FVector_NetQuantizeNormal", "UnrealSharp.CoreUObject.FVector");
        AddBlittableCustomStructPropertyTranslator("FVector4f", "UnrealSharp.CoreUObject.FVector4f");
        AddBlittableCustomStructPropertyTranslator("FQuatf4", "UnrealSharp.CoreUObject.FVector4f");
        AddBlittableCustomStructPropertyTranslator("FMatrix44f", "UnrealSharp.CoreUObject.FMatrix44f");
        AddBlittableCustomStructPropertyTranslator("FRotator", "UnrealSharp.CoreUObject.FRotator");
        AddBlittableCustomStructPropertyTranslator("FTransform", "UnrealSharp.CoreUObject.FTransform");
        
        AddBlittableCustomStructPropertyTranslator("FTimerHandle", "UnrealSharp.Engine.FTimerHandle");
        AddBlittableCustomStructPropertyTranslator("FInputActionValue", "UnrealSharp.EnhancedInput.FInputActionValue");
        
        AddPropertyTranslator(typeof(UhtArrayProperty), new ArrayPropertyTranslator());
        AddPropertyTranslator(typeof(UhtMapProperty), new MapPropertyTranslator());
        AddPropertyTranslator(typeof(UhtSetProperty), new SetPropertyTranslator());
        
        AddPropertyTranslator(typeof(UhtStructProperty), new BlittableStructPropertyTranslator());
        AddPropertyTranslator(typeof(UhtStructProperty), new StructPropertyTranslator());
        
        // Manually exported properties
        InclusionLists.BanProperty("UWorld", "GameState");
        InclusionLists.BanProperty("UWorld", "AuthorityGameMode");
    }
    
    public static PropertyTranslator? GetTranslator(UhtProperty property)
    {
        if (!RegisteredTranslators.TryGetValue(property.GetType(), out var translator))
        {
            return null;
        }
        
        foreach (PropertyTranslator propertyTranslator in translator!)
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
        if (RegisteredTranslators.TryGetValue(propertyType, out var translators))
        {
            translators!.Add(new BlittableTypePropertyTranslator(propertyType, managedType));
            return;
        }
        
        RegisteredTranslators.Add(propertyType, new List<PropertyTranslator> {new BlittableTypePropertyTranslator(propertyType, managedType)});
    }

    private static void AddPropertyTranslator(Type propertyClass, PropertyTranslator translator)
    {
        if (RegisteredTranslators.TryGetValue(propertyClass, out var translators))
        {
            translators!.Add(translator);
            return;
        }
        
        RegisteredTranslators.Add(propertyClass, new List<PropertyTranslator> {translator});
    }
    
    private static void AddBlittableCustomStructPropertyTranslator(string nativeName, string managedType)
    {
        AddPropertyTranslator(typeof(UhtStructProperty), new BlittableCustomStructTypePropertyTranslator(nativeName, managedType));
        ManuallyExportedTypes.Add(nativeName);
    }
}
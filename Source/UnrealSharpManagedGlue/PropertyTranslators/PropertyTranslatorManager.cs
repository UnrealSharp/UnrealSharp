using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.Model;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue.PropertyTranslators;

public static class PropertyTranslatorManager
{
    private static readonly Dictionary<Type, List<PropertyTranslator>?> RegisteredTranslators = new();
    public static SpecialTypeInfo SpecialTypeInfo { get; } = new();
    
    static PropertyTranslatorManager()
    {
        string? projectDirectory = GeneratorStatics.Factory.Session.ProjectDirectory;
        string configDirectory = Path.Combine(projectDirectory!, "Config");
        
        string pluginDirectory = Path.Combine(projectDirectory!, "Plugins");
        DirectoryInfo pluginDirInfo = new DirectoryInfo(pluginDirectory);
        
        IEnumerable<string> files = pluginDirInfo.GetFiles("*.uplugin", SearchOption.AllDirectories)
            .Select(x => x.DirectoryName!)
            .Select(x => Path.Combine(x, "Config"))
            .Concat(new List<string> { configDirectory })
            .Select(x => new DirectoryInfo(x))
            .Where(x => x.Exists)
            .SelectMany(x => x.GetFiles("*.UnrealSharpTypes.json", SearchOption.AllDirectories))
            .Select(x => x.FullName);
        
        foreach (string pluginFile in files)
        {
            using FileStream fileStream = File.OpenRead(pluginFile);
            try
            {
                TypeTranslationManifest? manifest = JsonSerializer.Deserialize<TypeTranslationManifest>(fileStream);
                AddTranslationManifest(manifest!);
            }
            catch (JsonException e)
            {
                Console.WriteLine($"Error reading {pluginFile}: {e.Message}");
            }
        }
        
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
#if !UE_5_6_OR_LATER
        AddPropertyTranslator(typeof(UhtObjectPtrProperty), worldContextObjectPropertyTranslator);
#endif
        AddPropertyTranslator(typeof(UhtObjectProperty), worldContextObjectPropertyTranslator);
        AddPropertyTranslator(typeof(UhtLazyObjectPtrProperty), worldContextObjectPropertyTranslator);
        
        ObjectPropertyTranslator objectPropertyTranslator = new();
        AddPropertyTranslator(typeof(UhtObjectPropertyBase), objectPropertyTranslator);
#if !UE_5_6_OR_LATER
        AddPropertyTranslator(typeof(UhtObjectPtrProperty), objectPropertyTranslator);
#endif
        AddPropertyTranslator(typeof(UhtObjectProperty), objectPropertyTranslator);
        AddPropertyTranslator(typeof(UhtLazyObjectPtrProperty), objectPropertyTranslator);
        
        AddPropertyTranslator(typeof(UhtInterfaceProperty), new InterfacePropertyTranslator());
        
        AddPropertyTranslator(typeof(UhtClassProperty), new SubclassOfPropertyTranslator());
#if !UE_5_6_OR_LATER
        AddPropertyTranslator(typeof(UhtClassPtrProperty), new SubclassOfPropertyTranslator());
#endif
        AddPropertyTranslator(typeof(UhtSoftClassProperty), new SoftClassPropertyTranslator());
        AddPropertyTranslator(typeof(UhtSoftObjectProperty), new SoftObjectPropertyTranslator());
        AddPropertyTranslator(typeof(UhtFieldPathProperty), new FieldPathPropertyTranslator());

        foreach ((string nativeName, string? managedType) in SpecialTypeInfo.Structs.BlittableTypes.Values)
        {
            if (managedType is null)
            {
                continue;
            }

            AddPropertyTranslator(typeof(UhtStructProperty), new BlittableCustomStructTypePropertyTranslator(nativeName, managedType));
        }

        AddPropertyTranslator(typeof(UhtArrayProperty), new ContainerPropertyTranslator("ArrayCopyMarshaller",
            "ArrayReadOnlyMarshaller",
            "ArrayMarshaller",
            "System.Collections.Generic.IReadOnlyList",
            "System.Collections.Generic.IList"));
        
        AddPropertyTranslator(typeof(UhtMapProperty), new ContainerPropertyTranslator("MapCopyMarshaller",
            "MapReadOnlyMarshaller",
            "MapMarshaller",
            "System.Collections.Generic.IReadOnlyDictionary",
            "System.Collections.Generic.IDictionary"));
        
        AddPropertyTranslator(typeof(UhtSetProperty), new ContainerPropertyTranslator("SetCopyMarshaller", 
            "SetReadOnlyMarshaller", 
            "SetMarshaller", 
            "System.Collections.Generic.IReadOnlySet", 
            "System.Collections.Generic.ISet"));
        
        AddPropertyTranslator(typeof(UhtOptionalProperty), new ContainerPropertyTranslator("OptionalMarshaller",
            "OptionalMarshaller",
            "OptionalMarshaller",
            "UnrealSharp.TOptional",
            "UnrealSharp.TOptional"));
        
        AddPropertyTranslator(typeof(UhtStructProperty), new BlittableStructPropertyTranslator());
        AddPropertyTranslator(typeof(UhtStructProperty), new StructPropertyTranslator());
        
        // Manually exported properties
        InclusionLists.BanProperty("UWorld", "GameState");
        InclusionLists.BanProperty("UWorld", "AuthorityGameMode");

        // Some reason == equality differs from .Equals
        InclusionLists.BanEquality("FRandomStream");

        // Renamed variables X/Y/Z to Pitch/Yaw/Roll
        InclusionLists.BanEquality("FRotator");

        // Fields not generating correctly
        InclusionLists.BanEquality("FVector3f");
        InclusionLists.BanEquality("FVector2f");
        InclusionLists.BanEquality("FVector4f");
        InclusionLists.BanEquality("FVector_NetQuantize");
        InclusionLists.BanEquality("FVector_NetQuantize10");
        InclusionLists.BanEquality("FVector_NetQuantize100");
        InclusionLists.BanEquality("FVector_NetQuantizeNormal");

        // Doesn't have any fields
        InclusionLists.BanEquality("FSubsystemCollectionBaseRef");

        // Custom arithmetic needed
        InclusionLists.BanArithmetic("FQuat");
    }

    public static void AddTranslationManifest(TypeTranslationManifest manifest)
    {
        foreach (string skippedStruct in manifest.Structs.CustomTypes)
        {
            SpecialTypeInfo.Structs.SkippedTypes.Add(skippedStruct);
        }
        
        foreach (BlittableStructInfo structInfo in manifest.Structs.BlittableTypes)
        {
            if (SpecialTypeInfo.Structs.NativelyCopyableTypes.ContainsKey(structInfo.Name))
            {
                throw new InvalidOperationException(
                    $"A struct cannot be both blittable and natively copyable: {structInfo.Name}");
            }
            
            if (SpecialTypeInfo.Structs.BlittableTypes.TryGetValue(structInfo.Name, out BlittableStructInfo existing))
            {
                if (structInfo.ManagedType is not null && existing.ManagedType is not null &&
                    structInfo.ManagedType != existing.ManagedType)
                {
                    throw new InvalidOperationException($"Duplicate struct name specified: {structInfo.Name}");
                }
            }
            else
            {
                SpecialTypeInfo.Structs.BlittableTypes.Add(structInfo.Name, structInfo);
            }
        }

        foreach (NativelyTranslatableStructInfo structInfo in manifest.Structs.NativelyTranslatableTypes)
        {
            if (SpecialTypeInfo.Structs.NativelyCopyableTypes.TryGetValue(structInfo.Name, out NativelyTranslatableStructInfo existing))
            {
                SpecialTypeInfo.Structs.NativelyCopyableTypes[structInfo.Name] = existing with { HasDestructor = existing.HasDestructor || structInfo.HasDestructor };
                continue;
            }
            
            if (SpecialTypeInfo.Structs.BlittableTypes.ContainsKey(structInfo.Name))
            {
                throw new InvalidOperationException(
                    $"A struct cannot be both blittable and natively copyable: {structInfo.Name}");
            }

            SpecialTypeInfo.Structs.NativelyCopyableTypes.Add(structInfo.Name, structInfo);
        }
    }
    
    public static PropertyTranslator? GetTranslator(this UhtProperty property)
    {
        if (!RegisteredTranslators.TryGetValue(property.GetType(), out List<PropertyTranslator>? translator))
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

    static void AddBlittablePropertyTranslator(Type propertyType, string managedType)
    {
        if (RegisteredTranslators.TryGetValue(propertyType, out List<PropertyTranslator>? translators))
        {
            translators!.Add(new BlittableTypePropertyTranslator(propertyType, managedType));
            return;
        }
        
        RegisteredTranslators.Add(propertyType, new List<PropertyTranslator>
        {
            new BlittableTypePropertyTranslator(propertyType, managedType) 
        });
    }

    static void AddPropertyTranslator(Type propertyClass, PropertyTranslator translator)
    {
        if (RegisteredTranslators.TryGetValue(propertyClass, out List<PropertyTranslator>? translators))
        {
            translators!.Add(translator);
            return;
        }
        
        RegisteredTranslators.Add(propertyClass, new List<PropertyTranslator> {translator});
    }
}

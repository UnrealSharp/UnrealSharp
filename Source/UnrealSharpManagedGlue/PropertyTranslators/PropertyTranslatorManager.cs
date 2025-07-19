using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Model;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public static class PropertyTranslatorManager
{
    private static readonly Dictionary<Type, List<PropertyTranslator>?> RegisteredTranslators = new();
    
    public static SpecialTypeInfo SpecialTypeInfo { get; } = new();
    
    static PropertyTranslatorManager()
    {
        var projectDirectory = Program.Factory.Session.ProjectDirectory;
        var configDirectory = Path.Combine(projectDirectory!, "Config");
        
        var pluginDirectory = Path.Combine(projectDirectory!, "Plugins");
        var pluginDirInfo = new DirectoryInfo(pluginDirectory);

        var files = pluginDirInfo.GetFiles("*.uplugin", SearchOption.AllDirectories)
            .Select(x => x.DirectoryName!)
            .Select(x => Path.Combine(x, "Config"))
            .Concat([configDirectory])
            .Select(x => new DirectoryInfo(x))
            .Where(x => x.Exists)
            .SelectMany(x => x.GetFiles("*.UnrealSharpTypes.json", SearchOption.AllDirectories))
            .Select(x => x.FullName);
        foreach (var pluginFile in files)
        {
            using var fileStream = File.OpenRead(pluginFile);
            try
            {
                var manifest = JsonSerializer.Deserialize<TypeTranslationManifest>(fileStream);
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
        
        AddPropertyTranslator(typeof(UhtClassProperty), new ClassPropertyTranslator());
#if !UE_5_6_OR_LATER
        AddPropertyTranslator(typeof(UhtClassPtrProperty), new ClassPropertyTranslator());
#endif
        AddPropertyTranslator(typeof(UhtSoftClassProperty), new SoftClassPropertyTranslator());
        AddPropertyTranslator(typeof(UhtSoftObjectProperty), new SoftObjectPropertyTranslator());
        AddPropertyTranslator(typeof(UhtFieldPathProperty), new FieldPathPropertyTranslator());

        foreach (var (nativeName, managedType) in SpecialTypeInfo.Structs.BlittableTypes.Values)
        {
            if (managedType is null)
            {
                continue;
            }

            AddBlittableCustomStructPropertyTranslator(nativeName, managedType);
        }

        AddPropertyTranslator(typeof(UhtArrayProperty), new ArrayPropertyTranslator());
        AddPropertyTranslator(typeof(UhtMapProperty), new MapPropertyTranslator());
        AddPropertyTranslator(typeof(UhtSetProperty), new SetPropertyTranslator());
        
        AddPropertyTranslator(typeof(UhtStructProperty), new BlittableStructPropertyTranslator());
        AddPropertyTranslator(typeof(UhtStructProperty), new StructPropertyTranslator());
        
        AddPropertyTranslator(typeof(UhtOptionalProperty), new OptionalPropertyTranslator());
        
        // Manually exported properties
        InclusionLists.BanProperty("UWorld", "GameState");
        InclusionLists.BanProperty("UWorld", "AuthorityGameMode");
    }

    public static void AddTranslationManifest(TypeTranslationManifest manifest)
    {
        foreach (var structInfo in manifest.Structs.BlittableTypes)
        {
            if (SpecialTypeInfo.Structs.NativelyCopyableTypes.ContainsKey(structInfo.Name))
            {
                throw new InvalidOperationException(
                    $"A struct cannot be both blittable and natively copyable: {structInfo.Name}");
            }
            
            if (SpecialTypeInfo.Structs.BlittableTypes.TryGetValue(structInfo.Name, out var existing))
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

        foreach (var structInfo in manifest.Structs.NativelyTranslatableTypes)
        {
            if (SpecialTypeInfo.Structs.NativelyCopyableTypes.TryGetValue(structInfo.Name, out var existing))
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
    }
}

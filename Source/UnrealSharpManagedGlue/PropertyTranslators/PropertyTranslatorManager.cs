using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using EpicGames.UHT.Types;
using UnrealSharp.Shared;
using UnrealSharpManagedGlue.Model;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue.PropertyTranslators;

public class PropertyTranslatorManager
{
    private readonly Dictionary<Type, List<PropertyTranslator>?> RegisteredTranslators = new();
    public SpecialTypeInfo SpecialTypeInfo { get; } = new();
    
    public void Initialize()
    {
        string? projectDirectory = SessionStatic.Generator.Factory.Session.ProjectDirectory;
        string configDirectory = Path.Combine(projectDirectory!, "Config");
        
        string pluginDirectory = Path.Combine(projectDirectory!, "Plugins");
        DirectoryInfo pluginDirInfo = new DirectoryInfo(pluginDirectory);
        
        IEnumerable<FileInfo> pluginFiles = pluginDirInfo.Exists
            ? pluginDirInfo.GetFiles("*.uplugin", SearchOption.AllDirectories)
            : Array.Empty<FileInfo>();

        IEnumerable<string> files = pluginFiles
            .Select(x => x.DirectoryName!)
            .Select(x => Path.Combine(x, "Config"))
            .Concat(new List<string> { Path.Combine(SessionStatic.Generator.PluginDirectory, "Config"), configDirectory })
            .Distinct(StringComparer.OrdinalIgnoreCase)
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
                ConsoleUtilities.Log($"Error reading {pluginFile}: {e.Message}");
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
        AddPropertyTranslator(typeof(UhtObjectProperty), worldContextObjectPropertyTranslator);
        AddPropertyTranslator(typeof(UhtLazyObjectPtrProperty), worldContextObjectPropertyTranslator);
        
        ObjectPropertyTranslator objectPropertyTranslator = new();
        AddPropertyTranslator(typeof(UhtObjectPropertyBase), objectPropertyTranslator);
        AddPropertyTranslator(typeof(UhtObjectProperty), objectPropertyTranslator);
        AddPropertyTranslator(typeof(UhtLazyObjectPtrProperty), objectPropertyTranslator);
        
        AddPropertyTranslator(typeof(UhtInterfaceProperty), new InterfacePropertyTranslator());
        
        AddPropertyTranslator(typeof(UhtClassProperty), new SubclassOfPropertyTranslator());
        AddPropertyTranslator(typeof(UhtSoftClassProperty), new SoftClassPropertyTranslator());
        AddPropertyTranslator(typeof(UhtSoftObjectProperty), new SoftObjectPropertyTranslator());
        AddPropertyTranslator(typeof(UhtFieldPathProperty), new FieldPathPropertyTranslator());

        foreach (BlittableStructInfo managedType in SpecialTypeInfo.Structs.BlittableTypes.Values)
        {
            if (managedType.ManagedType is null)
            {
                continue;
            }
            
            AddPropertyTranslator(typeof(UhtStructProperty), new BlittableCustomStructTypePropertyTranslator(managedType.Name, managedType.ManagedType));
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
        SessionStatic.Inclusions.BanProperty("UWorld", "GameState");
        SessionStatic.Inclusions.BanProperty("UWorld", "AuthorityGameMode");
        
        // Replicated properties handled by custom code
        SessionStatic.Inclusions.BanProperty("AActor", "bReplicates");
        SessionStatic.Inclusions.BanProperty("UActorComponent", "bReplicates");
        SessionStatic.Inclusions.BanFunction("UActorComponent", "SetIsReplicated");

        // Doesn't have any fields
        SessionStatic.Inclusions.BanArithmetic("FSubsystemCollectionBaseRef");
        SessionStatic.Inclusions.BanArithmetic("FRandomStream");
        SessionStatic.Inclusions.BanArithmetic("FRotator");
        SessionStatic.Inclusions.BanArithmetic("FVector3f");
        SessionStatic.Inclusions.BanArithmetic("FVector2f");
        SessionStatic.Inclusions.BanArithmetic("FVector4f");
        SessionStatic.Inclusions.BanArithmetic("FVector_NetQuantize");
        SessionStatic.Inclusions.BanArithmetic("FVector_NetQuantize10");
        SessionStatic.Inclusions.BanArithmetic("FVector_NetQuantize100");
        SessionStatic.Inclusions.BanArithmetic("FVector_NetQuantizeNormal");

        // Custom arithmetic needed
        SessionStatic.Inclusions.BanArithmetic("FQuat");

        // Renamed variable
        SessionStatic.Inclusions.BanConstructor("FPlane");

        // Has constructor with with defaults
        SessionStatic.Inclusions.BanConstructor("FGameplayTagContainer");
        SessionStatic.Inclusions.BanConstructor("FLinearColor");

        // Calls native to init
        SessionStatic.Inclusions.BanConstructor("FGameplayTag");
        SessionStatic.Inclusions.BanConstructor("FPrimaryAssetType");
    }
    public void AddTranslationManifest(TypeTranslationManifest manifest)
    {
        if (!manifest.Structs.CustomTypes.IsDefaultOrEmpty)
        {
            foreach (string skippedStruct in manifest.Structs.CustomTypes)
            {
                SpecialTypeInfo.Structs.SkippedTypes.Add(skippedStruct);
            }
        }

        if (!manifest.Structs.BlittableTypes.IsDefaultOrEmpty)
        {
            foreach (BlittableStructInfo structInfo in manifest.Structs.BlittableTypes)
            {
                if (SpecialTypeInfo.Structs.NativelyCopyableTypes.ContainsKey(structInfo.Name))
                {
                    throw new InvalidOperationException($"A struct cannot be both blittable and natively copyable: {structInfo.Name}");
                }
            
                if (SpecialTypeInfo.Structs.BlittableTypes.TryGetValue(structInfo.Name, out BlittableStructInfo existing))
                {
                    if (structInfo.ManagedType is not null && existing.ManagedType is not null && structInfo.ManagedType != existing.ManagedType)
                    {
                        throw new InvalidOperationException($"Duplicate struct name specified: {structInfo.Name}");
                    }
                }
                else
                {
                    SpecialTypeInfo.Structs.BlittableTypes.Add(structInfo.Name, structInfo);
                }
            } 
        }

        if (!manifest.Structs.NativelyTranslatableTypes.IsDefaultOrEmpty)
        {
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
    }
    
    public PropertyTranslator? GetTranslator(UhtProperty property)
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

    void AddBlittablePropertyTranslator(Type propertyType, string managedType)
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

    void AddPropertyTranslator(Type propertyClass, PropertyTranslator translator)
    {
        if (RegisteredTranslators.TryGetValue(propertyClass, out List<PropertyTranslator>? translators))
        {
            translators!.Add(translator);
            return;
        }
        
        RegisteredTranslators.Add(propertyClass, new List<PropertyTranslator> {translator});
    }
}

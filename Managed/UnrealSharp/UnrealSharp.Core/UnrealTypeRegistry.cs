using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace UnrealSharp.Core;

/// <summary>Generated type maps for native type lookup, scoped to the owning assembly.</summary>
public static class UnrealTypeRegistry
{
    public const DynamicallyAccessedMemberTypes Constructors =
        DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors;

    private static readonly ConditionalWeakTable<Assembly, Lazy<IReadOnlyDictionary<string, Type>>> TypeMaps = new();
    private static readonly ConditionalWeakTable<Assembly, ConcurrentDictionary<string, Type>> RegisteredTypes = new();

    public static void RegisterAssembly(Assembly assembly, Func<IReadOnlyDictionary<string, Type>> createTypeMap)
    {
        TypeMaps.GetValue(assembly, _ => new Lazy<IReadOnlyDictionary<string, Type>>(createTypeMap));
    }

    public static void RegisterType([DynamicallyAccessedMembers(Constructors)] Type type)
    {
        RegisteredTypes.GetOrCreateValue(type.Assembly)[type.FullName!] = type;
    }

    [return: DynamicallyAccessedMembers(Constructors)]
    [UnconditionalSuppressMessage("Trimming", "IL2068", Justification = "Generated maps preserve constructors, and RegisterType requires the same constructor annotations. Dictionary values cannot carry these annotations.")]
    public static Type? FindType(Assembly assembly, string fullName)
    {
        RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
        if (RegisteredTypes.TryGetValue(assembly, out var registered) && registered.TryGetValue(fullName, out Type? registeredType))
        {
            return registeredType;
        }

        return TypeMaps.TryGetValue(assembly, out var map) && map.Value.TryGetValue(fullName, out Type? type) ? type : null;
    }

    [return: DynamicallyAccessedMembers(Constructors)]
    [UnconditionalSuppressMessage("Trimming", "IL2073", Justification = "Native type handles are created from generated type maps or RegisterManagedType, both of which preserve constructors.")]
    internal static Type? GetTypeFromHandle(IntPtr handle) => GCHandleUtilities.GetObjectFromHandlePtr<Type>(handle);
}

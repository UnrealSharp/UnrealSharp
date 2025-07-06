using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace UnrealSharp.Core;

public static class GCHandleUtilities
{
    private static readonly ConcurrentDictionary<AssemblyLoadContext, ConcurrentDictionary<GCHandle, object>> StrongRefsByAssembly = new();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void OnAlcUnloading(AssemblyLoadContext alc)
    {
        StrongRefsByAssembly.TryRemove(alc, out _);
    }

    public static GCHandle AllocateStrongPointer(object value, object allocator)
    {
        return AllocateStrongPointer(value, allocator.GetType().Assembly);
    }
    
    public static GCHandle AllocateStrongPointer(object value, Assembly alc)
    {
        AssemblyLoadContext? assemblyLoadContext = AssemblyLoadContext.GetLoadContext(alc);
        
        if (assemblyLoadContext == null)
        {
            throw new InvalidOperationException("AssemblyLoadContext is null.");
        }

        return AllocateStrongPointer(value, assemblyLoadContext);
    }

    public static GCHandle AllocateStrongPointer(object value, AssemblyLoadContext loadContext)
    {
        GCHandle weakHandle = GCHandle.Alloc(value, GCHandleType.Weak);
        ConcurrentDictionary<GCHandle, object> strongReferences = StrongRefsByAssembly.GetOrAdd(loadContext, alcInstance =>
        {
            alcInstance.Unloading += OnAlcUnloading;
            return new ConcurrentDictionary<GCHandle, object>();
        });

        strongReferences.TryAdd(weakHandle, value);

        return weakHandle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GCHandle AllocateWeakPointer(object value) => GCHandle.Alloc(value, GCHandleType.Weak);
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GCHandle AllocatePinnedPointer(object value) => GCHandle.Alloc(value, GCHandleType.Pinned);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Free(GCHandle handle, Assembly? assembly)
    {
        if (assembly != null)
        {
            AssemblyLoadContext? assemblyLoadContext = AssemblyLoadContext.GetLoadContext(assembly);
            
            if (assemblyLoadContext == null)
            {
                throw new InvalidOperationException("AssemblyLoadContext is null.");
            }
            
            if (StrongRefsByAssembly.TryGetValue(assemblyLoadContext, out var strongReferences))
            {
                strongReferences.TryRemove(handle, out _);
            }
        }

        handle.Free();
    }
        
    public static T? GetObjectFromHandlePtr<T>(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
        {
            return default;
        }
        
        GCHandle subObjectGcHandle = GCHandle.FromIntPtr(handle);
        if (!subObjectGcHandle.IsAllocated)
        {
            return default;
        }
        
        object? subObject = subObjectGcHandle.Target;
        if (subObject is T typedObject)
        {
            return typedObject;
        }

        return default;
    }
}
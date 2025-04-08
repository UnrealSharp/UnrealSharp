using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace UnrealSharp.Core;

public static class GCHandleUtilities
{
    private static readonly ConcurrentDictionary<AssemblyLoadContext, ConcurrentDictionary<GCHandle, object>> StrongRefsByAssembly = new();

    private static AssemblyLoadContext? GetAssemblyLoadContext(object obj)
    {
        return AssemblyLoadContext.GetLoadContext(obj.GetType().Assembly);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void OnAlcUnloading(AssemblyLoadContext alc)
    {
        StrongRefsByAssembly.TryRemove(alc, out _);
    }

    public static GCHandle AllocateStrongPointer(object value, Assembly? alc = null)
    {
        AssemblyLoadContext? assemblyLoadContext;
        if (alc == null)
        {
            assemblyLoadContext = GetAssemblyLoadContext(value);
        }
        else
        {
            assemblyLoadContext = AssemblyLoadContext.GetLoadContext(alc)!;
        }
        
        if (assemblyLoadContext == null)
        {
            return GCHandle.Alloc(value, GCHandleType.Weak);
        }
        
        GCHandle weakHandle = GCHandle.Alloc(value, GCHandleType.Weak);
        ConcurrentDictionary<GCHandle, object> strongReferences = StrongRefsByAssembly.GetOrAdd(assemblyLoadContext, alcInstance =>
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

    public static void Free(GCHandle handle)
    {
        object? target = handle.Target;
        
        if (target != null)
        {
            AssemblyLoadContext? alc = GetAssemblyLoadContext(target);
            
            if (alc != null && StrongRefsByAssembly.TryGetValue(alc, out var strongReferences))
            {
                strongReferences.TryRemove(handle, out _);
            }
        }

        handle.Free();
    }
    
    public static object? GetObjectFromHandlePtr(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
        {
            return null;
        }
        
        GCHandle subObjectGcHandle = GCHandle.FromIntPtr(handle);
        return subObjectGcHandle.IsAllocated ? subObjectGcHandle.Target : null;
    }
        
    public static T? GetObjectFromHandlePtr<T>(IntPtr handle)
    {
        return (T?) GetObjectFromHandlePtr(handle);
    }
}
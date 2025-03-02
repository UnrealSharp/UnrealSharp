using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace UnrealSharp;

public static class GcHandleUtilities
{
    private static readonly ConcurrentDictionary<AssemblyLoadContext, ConcurrentDictionary<GCHandle, object>> StrongReferencesByAlc = new();

    private static AssemblyLoadContext? GetAssemblyLoadContext(object obj)
    {
        return AssemblyLoadContext.GetLoadContext(obj.GetType().Assembly);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void OnAlcUnloading(AssemblyLoadContext alc)
    {
        StrongReferencesByAlc.TryRemove(alc, out _);
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
        
        var weakHandle = GCHandle.Alloc(value, GCHandleType.Weak);
        var strongReferences = StrongReferencesByAlc.GetOrAdd(assemblyLoadContext, alcInstance =>
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
            var alc = GetAssemblyLoadContext(target);
            if (alc != null && StrongReferencesByAlc.TryGetValue(alc, out var strongReferences))
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
        return subObjectGcHandle.Target;
    }
        
    public static T? GetObjectFromHandlePtr<T>(IntPtr handle)
    {
        return (T?) GetObjectFromHandlePtr(handle);
    }
}
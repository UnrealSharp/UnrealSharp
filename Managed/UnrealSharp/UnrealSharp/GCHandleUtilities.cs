#nullable enable

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using UnrealSharp.Interop;

namespace UnrealSharp;

public static class GcHandleUtilities
{
    private static readonly ConditionalWeakTable<AssemblyLoadContext, object?> AlCsBeingUnloaded = new();
    private static readonly ConcurrentDictionary<AssemblyLoadContext, ConcurrentDictionary<GCHandle, object>> StrongReferencesByAlc = new();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool IsAlcBeingUnloaded(AssemblyLoadContext alc) => AlCsBeingUnloaded.TryGetValue(alc, out _);

    private static AssemblyLoadContext? GetAssemblyLoadContext(object obj)
    {
        return AssemblyLoadContext.GetLoadContext(obj.GetType().Assembly);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void OnAlcUnloading(AssemblyLoadContext alc)
    {
        AlCsBeingUnloaded.Add(alc, null);
        StrongReferencesByAlc.TryRemove(alc, out _);
    }

    public static GCHandle AllocateStrongPointer(object value)
    {
        if (!AlcReloadCfg.IsAlcReloadingEnabled)
        {
            return GCHandle.Alloc(value, GCHandleType.Normal);
        }

        var alc = GetAssemblyLoadContext(value);
        if (alc == null || IsAlcBeingUnloaded(alc))
        {
            return GCHandle.Alloc(value, GCHandleType.Weak);
        }

        var weakHandle = GCHandle.Alloc(value, GCHandleType.Weak);
        var strongReferences = StrongReferencesByAlc.GetOrAdd(alc, alcInstance =>
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
        if (AlcReloadCfg.IsAlcReloadingEnabled)
        {
            var target = handle.Target;
            if (target != null)
            {
                var alc = GetAssemblyLoadContext(target);
                if (alc != null && StrongReferencesByAlc.TryGetValue(alc, out var strongReferences))
                {
                    strongReferences.TryRemove(handle, out _);
                }
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
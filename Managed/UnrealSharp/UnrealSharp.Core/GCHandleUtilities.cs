using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace UnrealSharp.Core;

public static class GCHandleUtilities
{
    private static readonly ConcurrentDictionary<AssemblyLoadContext, ConcurrentDictionary<GCHandle, object>> StrongRefsByAssembly = new();
    
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
            return new ConcurrentDictionary<GCHandle, object>();
        });
            
        strongReferences.TryAdd(weakHandle, value);
        return weakHandle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GCHandle AllocateWeakPointer(object value) => GCHandle.Alloc(value, GCHandleType.Weak);

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
            
            if (StrongRefsByAssembly.TryGetValue(assemblyLoadContext, out ConcurrentDictionary<GCHandle, object>? strongReferences))
            {
                strongReferences.TryRemove(handle, out _);
            }
        }

        handle.Free();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FreeAssembly(Assembly assembly)
    {
        AssemblyLoadContext? assemblyLoadContext = AssemblyLoadContext.GetLoadContext(assembly);
        
        if (assemblyLoadContext == null)
        {
            throw new InvalidOperationException("AssemblyLoadContext is null.");
        }

        if (!StrongRefsByAssembly.TryRemove(assemblyLoadContext, out ConcurrentDictionary<GCHandle, object>? handles))
        {
            return;
        }
        
        foreach (GCHandle handle in handles.Keys)
        {
            handle.Free();
        }
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
    
    public static T? GetObjectFromHandlePtrFast<T>(IntPtr handle)
    {
        GCHandle subObjectGcHandle = GCHandle.FromIntPtr(handle);
        object? subObject = subObjectGcHandle.Target;
        return (T?)subObject;
    }
}
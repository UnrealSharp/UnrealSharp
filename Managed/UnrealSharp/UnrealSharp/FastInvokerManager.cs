using System.Reflection;
using System.Runtime.InteropServices;

namespace UnrealSharp;

public static class FastInvokerManager
{
    public static IntPtr CreateFastInvoker(MethodInfo method)
    {
        FastInvoker methodInvoker = new FastInvoker(method);
        Assembly assembly = method.Module.Assembly;
        
        if (MethodInvokers.TryGetValue(assembly, out var invoker))
        {
            invoker.Add(methodInvoker);
        }
        else
        {
            MethodInvokers.Add(assembly, [methodInvoker]);
        }
        
        return GCHandle.ToIntPtr(GcHandleUtilities.AllocateWeakPointer(methodInvoker));
    }
    
    public static FastInvoker? GetFastInvoker(GCHandle invokerHandle)
    {
        if (invokerHandle.IsAllocated)
        {
            return (FastInvoker) invokerHandle.Target;
        }
        
        throw new Exception("No MethodInvoker found for handle");
    }
    
    public static FastInvoker? GetFastInvoker(IntPtr invokerHandlePtr)
    {
        GCHandle invokerHandle = GCHandle.FromIntPtr(invokerHandlePtr);
        return GetFastInvoker(invokerHandle);
    }
    
    public static void FreeAllInvokersForAssembly(WeakReference<Assembly> assembly)
    {
        if (!assembly.TryGetTarget(out var foundAssembly))
        {
            return;
        }

        MethodInvokers.Remove(foundAssembly);
    }

    private static readonly Dictionary<Assembly, List<FastInvoker>> MethodInvokers = [];
}
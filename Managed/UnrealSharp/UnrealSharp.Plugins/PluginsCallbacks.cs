using System.Reflection;
using System.Runtime.InteropServices;

namespace UnrealSharp.Plugins;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct PluginsCallbacks
{
    public delegate* unmanaged<char*, NativeBool, nint> LoadPlugin;
    public delegate* unmanaged<char*, NativeBool> UnloadPlugin;
    
    [UnmanagedCallersOnly]
    private static nint ManagedLoadPlugin(char* assemblyPath, NativeBool isCollectible)
    {
        WeakReference? weakRef = PluginLoader.LoadPlugin(new string(assemblyPath), isCollectible.ToManagedBool());

        if (weakRef == null || !weakRef.IsAlive)
        {
            return IntPtr.Zero;
        };

        if (weakRef.Target is not Assembly assembly)
        {
            return IntPtr.Zero;
        }

        return GCHandle.ToIntPtr(GcHandleUtilities.AllocateWeakPointer(assembly));
    }

    [UnmanagedCallersOnly]
    private static NativeBool ManagedUnloadPlugin(char* assemblyPath)
    {
        string assemblyPathStr = new(assemblyPath);
        return PluginLoader.UnloadPlugin(assemblyPathStr).ToNativeBool();
    }

    public static PluginsCallbacks Create()
    {
        return new PluginsCallbacks
        {
            LoadPlugin = &ManagedLoadPlugin,
            UnloadPlugin = &ManagedUnloadPlugin,
        };
    }
}
using System.Reflection;
using System.Runtime.InteropServices;
using UnrealSharp.Interop;
using UnrealSharp.Logging;

namespace UnrealSharp.Plugins;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct PluginsCallbacks
{
    public delegate* unmanaged<char*, nint> LoadPlugin;
    public delegate* unmanaged<char*, NativeBool> UnloadPlugin;

    [UnmanagedCallersOnly]
    private static unsafe nint ManagedLoadPlugin(char* assemblyPath)
    {
        var weakRef = PluginLoader.LoadPlugin(new string(assemblyPath), true);

        if (weakRef == null) return default;
        if (!weakRef.IsAlive) return default;
        if (weakRef.Target is not Assembly assembly) return default;

        return GCHandle.ToIntPtr(GcHandleUtilities.AllocateWeakPointer(assembly));
    }

    [UnmanagedCallersOnly]
    private static unsafe NativeBool ManagedUnloadPlugin(char* assemblyPath)
    {
        string assemblyPathStr = new(assemblyPath);
        return PluginLoader.UnloadPlugin(assemblyPathStr).ToNativeBool();
    }

    public static PluginsCallbacks Create()
    {
        return new ()
        {
            LoadPlugin = &ManagedLoadPlugin,
            UnloadPlugin = &ManagedUnloadPlugin,
        };
    }
}
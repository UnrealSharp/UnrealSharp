using System.Reflection;
using System.Runtime.InteropServices;
using UnrealSharp.Core;

namespace UnrealSharp.Plugins;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct PluginsCallbacks
{
    public delegate* unmanaged<char*, NativeBool, nint> LoadPlugin;
    public delegate* unmanaged<char*, NativeBool> UnloadPlugin;
    
    [UnmanagedCallersOnly]
    private static nint ManagedLoadPlugin(char* assemblyPath, NativeBool isCollectible)
    {
        Assembly? newPlugin = PluginLoader.LoadPlugin(new string(assemblyPath), isCollectible.ToManagedBool());

        if (newPlugin == null)
        {
            return IntPtr.Zero;
        };

        return GCHandle.ToIntPtr(GCHandleUtilities.AllocateStrongPointer(newPlugin, newPlugin));
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
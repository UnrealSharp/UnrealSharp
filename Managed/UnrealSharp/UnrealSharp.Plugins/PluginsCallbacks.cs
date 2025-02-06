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
    private static nint ManagedLoadPlugin(char* assemblyPath)
    {
        WeakReference? weakRef = PluginLoader.LoadPlugin(new string(assemblyPath), true);

        if (weakRef == null || !weakRef.IsAlive)
        {
            return IntPtr.Zero;
        }

        return GCHandle.ToIntPtr(GcHandleUtilities.AllocateWeakPointer(weakRef.Target!));
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
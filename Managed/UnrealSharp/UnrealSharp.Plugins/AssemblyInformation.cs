using System.Reflection;

namespace UnrealSharp.Plugins;

public readonly struct AssemblyInformation(Assembly assembly, PluginLoadContextWrapper pluginLoadContextWrapper)
{
    public Assembly Assembly { get; } = assembly;
    public PluginLoadContextWrapper PluginLoadContextWrapper { get; } = pluginLoadContextWrapper;

    public bool IsValid => Assembly != null && PluginLoadContextWrapper is { IsAlive: true };
    public string Name => Assembly.GetName().Name!;
}
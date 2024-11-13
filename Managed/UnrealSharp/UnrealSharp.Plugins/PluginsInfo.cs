using System.Reflection;
using System.Runtime.Loader;

namespace UnrealSharp.Plugins;

internal static class PluginsInfo
{
    public static readonly List<PluginLoadContextWrapper> LoadedPlugins = [];
    public static readonly List<AssemblyName> SharedAssemblies = [];
    public static readonly AssemblyLoadContext MainLoadContext = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()) ?? AssemblyLoadContext.Default;
}
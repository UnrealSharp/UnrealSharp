using System.Reflection;
using System.Runtime.Loader;
using UnrealSharp.Binds;
using UnrealSharp.Core;
using UnrealSharp.Log;

namespace UnrealSharp.Plugins;

public static class AssemblyCache
{
	private static readonly Dictionary<string, WeakReference> LoadedAssemblies = new();
	
	static AssemblyCache()
	{
		AddAssembly(typeof(PluginLoader).Assembly);
		AddAssembly(typeof(NativeBinds).Assembly);
		AddAssembly(typeof(UnrealSharpObject).Assembly);
		AddAssembly(typeof(UnrealSharpModule).Assembly);
		AddAssembly(typeof(CustomLog).Assembly);
	}
    
	public static void AddAssembly(Assembly assembly)
	{
		LoadedAssemblies[assembly.GetName().Name!] = new WeakReference(assembly);
	}
	
	public static void RemoveAssembly(string assemblyName)
	{
		LoadedAssemblies.Remove(assemblyName);
	}
	
	public static Assembly? GetAssembly(string assemblyName)
	{
		if (LoadedAssemblies.TryGetValue(assemblyName, out var assemblyRef) && assemblyRef.IsAlive)
		{
			return (Assembly)assemblyRef.Target!;
		}
		
		return null;
	}
}
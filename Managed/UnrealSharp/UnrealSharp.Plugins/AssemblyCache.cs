using System.Reflection;
using System.Runtime.Loader;
using UnrealSharp.Binds;
using UnrealSharp.Core;
using UnrealSharp.Log;

namespace UnrealSharp.Plugins;

public static class AssemblyCache
{
	private static readonly Dictionary<string, List<WeakReference<Assembly>>> LoadedAssemblies = new();
	
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
		string assemblyName = assembly.GetName().Name!;
		
		if (!LoadedAssemblies.TryGetValue(assemblyName, out List<WeakReference<Assembly>>? assemblies))
		{
			assemblies = new List<WeakReference<Assembly>>();
			LoadedAssemblies[assemblyName] = assemblies;
		}
		
		assemblies.Add(new WeakReference<Assembly>(assembly));
	}
	
	public static void RemoveAssembly(string assemblyName)
	{
		LoadedAssemblies.Remove(assemblyName);
	}
	
	public static Assembly? GetUniqueAssembly(string assemblyName)
	{
		if (!LoadedAssemblies.TryGetValue(assemblyName, out List<WeakReference<Assembly>>? assemblies))
		{
			return null;
		}

		if (assemblies.Count == 0)
		{
			return null;
		}
		
		WeakReference<Assembly> assemblyReference = assemblies[0];
		if (!assemblyReference.TryGetTarget(out Assembly? assembly))
		{
			return null;
		}
		
		return assembly;
	}
	
	public static Assembly? GetAssembly(string assemblyName, AssemblyLoadContext loadContext)
	{
		if (!LoadedAssemblies.TryGetValue(assemblyName, out List<WeakReference<Assembly>>? assemblies))
		{
			return null;
		}

		Assembly? fallbackCandidate = null;

		foreach (WeakReference<Assembly> weakAssembly in assemblies)
		{
			if (!weakAssembly.TryGetTarget(out Assembly? assembly))
			{
				continue;
			}

			AssemblyLoadContext foundLoadContext = AssemblyLoadContext.GetLoadContext(assembly)!;

			if (foundLoadContext == loadContext)
			{
				return assembly;
			}

			if (!foundLoadContext.IsCollectible)
			{
				fallbackCandidate = assembly;
				continue;
			}

			if (loadContext.IsCollectible && fallbackCandidate == null)
			{
				fallbackCandidate = assembly;
			}
		}

		return fallbackCandidate;
	}
}
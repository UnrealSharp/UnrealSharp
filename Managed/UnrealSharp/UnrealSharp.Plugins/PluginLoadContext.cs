using System.Reflection;
using System.Runtime.Loader;

namespace UnrealSharp.Plugins;

public class PluginLoadContext : AssemblyLoadContext
{
	private readonly AssemblyDependencyResolver _resolver;

	public PluginLoadContext(string pluginName, AssemblyDependencyResolver resolver, bool isCollectible) : base(pluginName, isCollectible)
	{
		_resolver = resolver;
	}

	protected override Assembly? Load(AssemblyName assemblyName)
	{
		if (string.IsNullOrEmpty(assemblyName.Name))
		{
			return null;
		}

		Assembly? loadedAssembly = AssemblyCache.GetAssembly(assemblyName.Name!, this);
		if (loadedAssembly != null)
		{
			return loadedAssembly;
		}

		string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
		if (string.IsNullOrEmpty(assemblyPath))
		{
			return null;
		}

		using FileStream assemblyFile = File.Open(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read);
		string pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");

		Assembly? newAssembly;
		if (!File.Exists(pdbPath))
		{
			newAssembly = LoadFromAssemblyPath(assemblyPath);
		}
		else
		{
			using FileStream pdbFile = File.Open(pdbPath, FileMode.Open, FileAccess.Read, FileShare.Read);
			newAssembly = LoadFromStream(assemblyFile, pdbFile);
		}
        
		AssemblyCache.AddAssembly(newAssembly);
		return newAssembly;
	}
}
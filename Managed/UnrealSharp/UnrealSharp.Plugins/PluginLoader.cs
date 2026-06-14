using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnrealSharp.Engine.Core.Modules;

namespace UnrealSharp.Plugins;

public static class PluginLoader
{
	private static readonly Dictionary<string, Plugin> Plugins = [];

	public static Assembly? LoadPlugin(string assemblyPath, bool isCollectible)
	{
		try
		{
			AssemblyName assemblyName = new AssemblyName(Path.GetFileNameWithoutExtension(assemblyPath));

			if (Plugins.TryGetValue(assemblyName.Name!, out Plugin? loadedPlugin))
			{
				return (Assembly)loadedPlugin.Assembly!.Target!;
			}

			Plugin plugin = new Plugin(assemblyName, isCollectible, assemblyPath);
			Plugins.Add(assemblyName.Name!, plugin);

			if (!plugin.Load())
			{
				Plugins.Remove(assemblyName.Name!);
				throw new InvalidOperationException($"Failed to load plugin: {assemblyName}");
			}

			LogUnrealSharpPlugins.Log($"Successfully loaded plugin: '{assemblyName}' at '{assemblyPath}'");
			return (Assembly)plugin.Assembly!.Target!;
		}
		catch (Exception ex)
		{
			LogUnrealSharpPlugins.LogError($"An error occurred while loading the plugin: {ex.Message}");
		}

		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference? RemovePlugin(string assemblyName)
	{
		if (!Plugins.Remove(assemblyName, out Plugin? value))
		{
			return null;
		}

		return value.Unload();
	}

	public static void UnloadPlugin(string assemblyPath)
	{
		TaskTracker.WaitForAllActiveTasks();

		string assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
		WeakReference? weakAlc = RemovePlugin(assemblyName);

		if (weakAlc == null)
		{
			LogUnrealSharpPlugins.Log($"Plugin {assemblyName} is not loaded or already removed from registry.");
			return;
		}

		try
		{
			LogUnrealSharpPlugins.Log($"Unloading plugin {assemblyName}...");

			Stopwatch stopWatch = Stopwatch.StartNew();

			int maxAttempts = 8;
			int attempt = 0;

			while (weakAlc.IsAlive && attempt < maxAttempts)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();

				if (!weakAlc.IsAlive)
				{
					break;
				}

				Thread.Sleep(1);
				attempt++;
			}

			if (weakAlc.IsAlive)
			{
				// https://github.com/dotnet/runtime/issues/124876
				LogUnrealSharpPlugins.LogWarning(
					$"'{assemblyName}' did not fully unload. " +
					"A known Visual Studio/Rider debugger issue may be holding strong references to assembly types, preventing the old assembly from being collected. " +
					"Hot reload will continue to work with some additional memory overhead. " +
					"Re-attaching the debugger usually releases these references and allows the assembly to be GC'd on the next hot reload.");
				return;
			}

			LogUnrealSharpPlugins.Log($"{assemblyName} unloaded successfully in {stopWatch.ElapsedMilliseconds}ms.");
		}
		catch (Exception exception)
		{
			LogUnrealSharpPlugins.LogError($"An error occurred while unloading the plugin: {exception}");
		}
	}

	public static Plugin? FindPlugin(Type type)
	{
		Assembly assembly = type.Assembly;
		return FindPlugin(assembly);
	}

	public static Plugin? FindPlugin(Assembly assembly)
	{
		return FindPlugin(assembly.GetName().Name!);
	}

	public static Plugin? FindPlugin(string assemblyName)
	{
		return Plugins.GetValueOrDefault(assemblyName);
	}

	public static T FindModule<T>() where T : class, IModuleInterface
	{
		Plugin plugin = FindPlugin(typeof(T))!;
		return plugin.GetModule<T>();
	}
}
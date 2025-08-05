using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.Utilities;

public static class PluginUtilities
{
    private const string ProjectGlueName = "<ProjectGlue>";
    private static readonly Dictionary<string, PluginDirInfo> PluginDirs = new();
    private static readonly Dictionary<string, HashSet<string>> PluginDependencies = new();

    public static HashSet<string> GetPackageDependencies(this UhtPackage package)
    {
        var pluginName = package.IsPlugin() ? package.GetPluginDirectory().PluginName : ProjectGlueName;
        if (PluginDependencies.TryGetValue(pluginName, out HashSet<string>? dependencies))
        {
            return dependencies;
        }

        dependencies = package.GetHeaderFiles()
                .SelectMany(x => x.ReferencedHeadersLocked)
                .SelectMany(x => x.GetPackages())
                .Where(x => !x.IsPartOfEngine())
                .Where(x => x.IsPlugin())
                .Select(x => x.GetPluginDirectory())
                .Select(x => x.PluginName)
                .Where(x => x != pluginName)
                .ToHashSet();
        PluginDependencies.Add(pluginName, dependencies);
        return dependencies;
    }

    public static PluginDirInfo GetPluginDirectory(this UhtPackage package)
    {
        if (PluginDirs.TryGetValue(package.SourceName, out var pluginDirectory))
        {
            return pluginDirectory;
        }

        var currentDirectory = new DirectoryInfo(package.GetModule().BaseDirectory);
        while (currentDirectory is not null)
        {
            var pluginFile = currentDirectory.GetFiles("*.uplugin", SearchOption.TopDirectoryOnly)
                    .SingleOrDefault();
            if (pluginFile is not null)
            {

                var info = new PluginDirInfo(Path.GetFileNameWithoutExtension(pluginFile.Name),
                        currentDirectory.FullName);
                PluginDirs.Add(package.SourceName, info);
                return info;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException($"Could not find plugin directory for {package.SourceName}");
    }

    public static IEnumerable<string> GetPluginDependencyPaths(string pluginName)
    {
        if (!PluginDependencies.TryGetValue(pluginName, out HashSet<string>? dependencies))
        {
            return [];
        }

        return dependencies
                .Select(x => PluginDirs[x])
                .Select(x => x.GlueProjectPath);
    }

    public static IEnumerable<string> GetProjectDependencyPaths()
    {
        return GetPluginDependencyPaths(ProjectGlueName);
    }
}

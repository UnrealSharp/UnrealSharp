﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Model;

namespace UnrealSharpScriptGenerator.Utilities;

public static class PluginUtilities
{
    public static readonly Dictionary<UhtPackage, ProjectDirInfo> PluginInfo = new();
    
    private static readonly Dictionary<string, string> ExtractedEngineModules = new();

    static PluginUtilities()
    {
        string? projectDirectory = Program.Factory.Session.ProjectDirectory;
        string pluginDirectory = Path.Combine(projectDirectory!, "Plugins");
        DirectoryInfo pluginDirInfo = new DirectoryInfo(pluginDirectory);
        
        IEnumerable<(string DirectoryName, string FullName)> files = pluginDirInfo.GetFiles("*.uplugin", SearchOption.AllDirectories)
            .Select(x => x.DirectoryName!)
            .Select(x => (DirectoryName: x, ConfigPath: Path.Combine(x, "Config")))
            .Select(x => (x.DirectoryName, ConfigDir: new DirectoryInfo(x.ConfigPath)))
            .Where(x => x.ConfigDir.Exists)
            .SelectMany(x => x.ConfigDir.GetFiles("*.ExtractedModules.json", SearchOption.AllDirectories),
                (x, y) => (x.DirectoryName, FileInfo: y))
            .Select(x => (x.DirectoryName, x.FileInfo.FullName));
        
        foreach ((string pluginDir, string pluginFile) in files)
        {
            using FileStream fileStream = File.OpenRead(pluginFile);
            try
            {
                List<string>? manifest = JsonSerializer.Deserialize<List<string>>(fileStream);
                foreach (string module in manifest!)
                {
                    ExtractedEngineModules.Add($"/Script/{module}", pluginDir);
                }
            }
            catch (JsonException e)
            {
                Console.WriteLine($"Error reading {pluginFile}: {e.Message}");
            }
        }
    }

    public static ProjectDirInfo FindOrAddProjectInfo(this UhtPackage package)
    {
        if (PluginInfo.TryGetValue(package, out ProjectDirInfo plugin))
        {
            return plugin;
        }

        ProjectDirInfo info;
        HashSet<string> dependencies = [];
        if (package.IsPartOfEngine())
        {
            if (ExtractedEngineModules.TryGetValue(package.SourceName, out string? pluginPath))
            {
                DirectoryInfo pluginDir = new(pluginPath);
                info = new ProjectDirInfo(pluginDir.Name, pluginPath, dependencies);
            }
            else
            {
                info = new ProjectDirInfo("Engine", Program.EngineGluePath, dependencies); 
            }
        }
        else
        {

            string baseDirectory = package.GetModule().BaseDirectory;
            DirectoryInfo? currentDirectory = new DirectoryInfo(baseDirectory);

            FileInfo? projectFile = null;
            while (currentDirectory is not null)
            {
                FileInfo[] foundFiles = currentDirectory.GetFiles("*.*", SearchOption.TopDirectoryOnly);
                projectFile = foundFiles.FirstOrDefault(f =>
                    f.Extension.Equals(".uplugin", StringComparison.OrdinalIgnoreCase) ||
                    f.Extension.Equals(".uproject", StringComparison.OrdinalIgnoreCase));

                if (projectFile is not null)
                {
                    break;
                }

                currentDirectory = currentDirectory.Parent;
            }

            if (projectFile is null)
            {
                throw new InvalidOperationException(
                    $"Could not find .uplugin or .uproject file for package {package.SourceName} in {baseDirectory}");
            }
            info = new ProjectDirInfo(Path.GetFileNameWithoutExtension(projectFile.Name), currentDirectory!.FullName, dependencies);
        }

        PluginInfo.Add(package, info);
        
        foreach (UhtHeaderFile header in package.GetHeaderFiles())
        {
            HashSet<UhtHeaderFile> referencedHeaders = header.References.ReferencedHeaders;
            referencedHeaders.UnionWith(header.ReferencedHeadersNoLock);
            
            foreach (UhtHeaderFile refHeader in referencedHeaders)
            {
                foreach (UhtPackage refPackage in refHeader.GetPackages())
                {
                    if (refPackage == package)
                    {
                        continue;
                    }

                    if (refPackage.IsPartOfEngine())
                    {
                        if (!ExtractedEngineModules.TryGetValue(refPackage.SourceName, out string? pluginPath))
                        {
                            continue;
                        }

                        if (info.IsPartOfEngine)
                        {
                            DirectoryInfo pluginDir = new(pluginPath);
                            info = new ProjectDirInfo(pluginDir.Name, pluginPath, dependencies);
                            PluginInfo[package] = info;
                        }
                    }
                    
                    
                    ProjectDirInfo projectInfo = refPackage.FindOrAddProjectInfo();
                    if (info.GlueCsProjPath == projectInfo.GlueCsProjPath)
                    {
                        continue;
                    }
                    
                    dependencies.Add(projectInfo.GlueCsProjPath);
                }
            }
        }
        
        return info;
    }
}

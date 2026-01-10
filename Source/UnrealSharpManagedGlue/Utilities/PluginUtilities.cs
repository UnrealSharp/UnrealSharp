using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using EpicGames.UHT.Types;

namespace UnrealSharpManagedGlue.Utilities;

public static class PluginUtilities
{
    public static readonly Dictionary<UhtPackage, ProjectDirInfo> PluginInfo = new();
    
    private static readonly Dictionary<string, string> ExtractedEngineModules = new();

    static PluginUtilities()
    {
        string? projectDirectory = GeneratorStatics.Factory.Session.ProjectDirectory;
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
        HashSet<string> dependencies = new ();
        
        if (package.IsPartOfEngine())
        {
            if (ExtractedEngineModules.TryGetValue(package.SourceName, out string? pluginPath))
            {
                DirectoryInfo pluginDir = new(pluginPath);
                info = new ProjectDirInfo(pluginDir.Name, pluginPath, dependencies);
            }
            else
            {
                info = new ProjectDirInfo("Engine", GeneratorStatics.EngineGluePath, dependencies); 
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
                throw new InvalidOperationException($"Could not find .uplugin or .uproject file for package {package.SourceName} in {baseDirectory}");
            }
            
            string fileName = Path.GetFileNameWithoutExtension(projectFile.Name);
            info = new ProjectDirInfo(fileName, currentDirectory!.FullName, dependencies);
        }

        PluginInfo.Add(package, info);
        
        void TryAddDependency(UhtPackage referencePackage)
        {
            if (referencePackage == package)
            {
                return;
            }

            if (referencePackage.IsPartOfEngine())
            {
                if (!ExtractedEngineModules.TryGetValue(referencePackage.SourceName, out string? pluginPath))
                {
                    return;
                }

                if (info.IsPartOfEngine)
                {
                    DirectoryInfo pluginDir = new(pluginPath);
                    info = new ProjectDirInfo(pluginDir.Name, pluginPath, dependencies);
                    PluginInfo[package] = info;
                }
            }
            
            ProjectDirInfo projectInfo = referencePackage.FindOrAddProjectInfo();
            if (info.GlueCsProjPath == projectInfo.GlueCsProjPath)
            {
                return;
            }
            
            dependencies.Add(projectInfo.GlueCsProjPath);
        }

        CSharpExporter.ForEachChildRecursive(package, childType =>
        {
            UhtEngineType engineType = childType.EngineType;

            if (engineType is UhtEngineType.Class or UhtEngineType.ScriptStruct)
            {
                UhtStruct foundClass = (UhtStruct) childType;
                
                if (foundClass.Super == null)
                {
                    return;
                }
                
                TryAddDependency(foundClass.Super!.Package);
            }
            else if (childType.EngineType == UhtEngineType.Property)
            {
                UhtProperty property = (UhtProperty) childType;
                foreach (UhtType referencedType in property.EnumerateReferencedTypes())
                {
                    TryAddDependency(referencedType.Package);
                }
            }
        });
        
        return info;
    }
}

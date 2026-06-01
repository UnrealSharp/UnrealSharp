using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnrealSharp.Automation.Utilities;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue;

public static class GlueModuleFactory
{
    private static readonly Dictionary<string, List<string>> ModuleDependencies;

    static GlueModuleFactory()
    {
        Dictionary<string, List<string>>? moduleDependencies = JsonUtilities.DeserializeObjectFromJson<Dictionary<string, List<string>>>(nameof(ModuleDependencies));
        
        if (moduleDependencies == null)
        {
            moduleDependencies = new Dictionary<string, List<string>>();
        }
        
        ModuleDependencies = moduleDependencies;
    }
    
    public static void CreateGlueProjects()
    {
        bool anyProjectChanges = false;
        foreach (ModuleInfo moduleInfo in ModuleUtilities.PackageToModuleInfo.Values)
        {
            if (!moduleInfo.Module.ShouldExportPackage() || moduleInfo.IsPartOfEngine)
            {
                continue;
            }
            
            bool recentlyCreatedModule = false;
            if (!File.Exists(moduleInfo.CsProjPath))
            {
                CreateGlueModule(moduleInfo);
                recentlyCreatedModule = true;
            }
            
            List<string> pluginDependencies = moduleInfo.Dependencies != null ? moduleInfo.Dependencies.Select(Path.GetFullPath).ToList() : new List<string>();
        
            if (!recentlyCreatedModule && ModuleDependencies.TryGetValue(moduleInfo.ProjectName, out List<string>? existingDependencies))
            {
                if (existingDependencies.OrderBy(d => d).SequenceEqual(pluginDependencies.OrderBy(d => d)))
                {
                    LoggerUtilities.LogUnrealSharpInfo($"No changes in plugin dependencies for {moduleInfo.ProjectName}, skipping update.");
                    return;
                }
            }
            
            AddPluginDependencies(moduleInfo, pluginDependencies);
            ModuleDependencies[moduleInfo.ProjectName] = pluginDependencies;
            anyProjectChanges = true;
        }

        if (!anyProjectChanges)
        {
            return;
        }
        
        BuildGlueProjects();
        JsonUtilities.SerializeObjectToJson(ModuleDependencies, nameof(ModuleDependencies));
    }

    private static void BuildGlueProjects()
    {
        List<KeyValuePair<string, string>> commandArgs = new List<KeyValuePair<string, string>>
        {
            new("TargetConfiguration", GeneratorStatics.TargetConfiguration.ToString()),
            new("TargetType", GeneratorStatics.TargetType.ToString()),
            new("OutputDirectory", PathUtilities.BuildOutputPath(GeneratorStatics.Factory.Session.ProjectDirectory!)),
        };
        
        UnrealSharpAutomationUtilities.InvokeUnrealSharpAutomation("BuildUserGlue", commandArgs);
    }

    private static void CreateGlueModule(ModuleInfo moduleInfo)
    {
        string csprojPath = moduleInfo.CsProjPath;
        
        List<KeyValuePair<string, string>> arguments = new List<KeyValuePair<string, string>>
        {
            new("ProjectName", moduleInfo.ProjectName),
            new("ProjectFolder", Path.GetDirectoryName(csprojPath)!),
            new("ProjectRoot", moduleInfo.ModuleRoot),
            new("SkipIncludeAnalyzers", "true"),
        };
        
        if (moduleInfo.Module.IsEditorOnly())
        {
            arguments.Add(new KeyValuePair<string, string>("EditorOnly", "true"));
        }
            
        if (!UnrealSharpAutomationUtilities.InvokeUnrealSharpAutomation("GenerateProject", arguments))
        {
            throw new InvalidOperationException($"Failed to create project file at {csprojPath}");
        }
            
        ConsoleUtilities.Log($"Successfully created project file: {csprojPath}");
    }

    private static void AddPluginDependencies(ModuleInfo moduleInfo, List<string>? pluginDependencies)
    {
        List<KeyValuePair<string, string>> arguments = new()
        {
            new KeyValuePair<string, string>("ProjectPath", moduleInfo.CsProjPath),
        };
        
        if (pluginDependencies != null)
        {
            foreach (string path in pluginDependencies)
            {
                arguments.Add(new KeyValuePair<string, string>("Dependencies", path));
            }
        }
        
        UnrealSharpAutomationUtilities.InvokeUnrealSharpAutomation("UpdateProjectDependencies", arguments);
    }
}
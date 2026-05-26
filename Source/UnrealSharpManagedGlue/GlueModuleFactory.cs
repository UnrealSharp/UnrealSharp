using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnrealSharp.Automation.Utilities;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue;

public static class GlueModuleFactory
{
    public static void CreateGlueProjects()
    {
        foreach (ModuleInfo moduleInfo in ModuleUtilities.PackageToModuleInfo.Values)
        {
            if (!moduleInfo.Module.ShouldExportPackage() || moduleInfo.IsPartOfEngine)
            {
                continue;
            }
        
            if (!File.Exists(moduleInfo.CsProjPath))
            {
                CreateGlueModule(moduleInfo);
            }

            AddPluginDependencies(moduleInfo);
        }
        
        BuildGlueProjects();
    }

    private static void BuildGlueProjects()
    {
        List<KeyValuePair<string, string>> commandArgs = new List<KeyValuePair<string, string>>
        {
            new("BuildConfig", GeneratorStatics.BuildConfiguration.ToString()),
            new("TargetType", GeneratorStatics.BuildTarget.ToString()),
            new("OutputDirectory", PathUtilities.BuildOutputPath(GeneratorStatics.Factory.Session.ProjectDirectory!)),
        };
        
        UnrealSharpAutomationUtilities.InvokeUnrealSharpAutomation("BuildUserGlue", commandArgs);
    }

    private static void CreateGlueModule(ModuleInfo moduleInfo)
    {
        string csprojPath = moduleInfo.CsProjPath;
        string projectDirectory = Path.GetDirectoryName(csprojPath)!;
        
        List<KeyValuePair<string, string>> arguments = new List<KeyValuePair<string, string>>
        {
            new("ProjectName", moduleInfo.ProjectName),
            new("ProjectFolder", projectDirectory),
            new("SkipSolutionGeneration", "true"),
            new("SkipUSharpProjSetup", "true"),
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

    private static void AddPluginDependencies(ModuleInfo moduleInfo)
    {
        List<string> pluginDependencies = moduleInfo.Dependencies != null ? moduleInfo.Dependencies.Select(Path.GetFullPath).ToList() : new List<string>();
        
        List<KeyValuePair<string, string>> arguments = new()
        {
            new KeyValuePair<string, string>("ProjectPath", moduleInfo.CsProjPath),
        };
        
        foreach (string path in pluginDependencies)
        {
            arguments.Add(new KeyValuePair<string, string>("Dependencies", path));
        }

        UnrealSharpAutomationUtilities.InvokeUnrealSharpAutomation("UpdateProjectDependencies", arguments);
    }
}
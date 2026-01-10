using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EpicGames.UHT.Utils;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue;

public static class GlueModuleFactory
{
    private const string ModuleDependenciesFileName = "ModuleDependencies";
    private static Dictionary<string, List<string>>? _moduleDependencies;
    
    public static void CreateGlueProjects()
    {
        LoadModuleDependencies();
        
        Dictionary<string, ProjectDirInfo> mergedModules = new();
        
        bool hasProjectGlue = false;
        foreach (ProjectDirInfo pluginDir in PluginUtilities.PluginInfo.Values)
        {
            if (pluginDir.IsPartOfEngine)
            {
                continue;
            }
            
            hasProjectGlue |= pluginDir.IsUProject;

            if (!mergedModules.TryGetValue(pluginDir.ProjectRoot, out ProjectDirInfo foundProjectDirInfo))
            {
                mergedModules[pluginDir.ProjectRoot] = pluginDir;
            }
            else
            {
                if (pluginDir.Dependencies == null || pluginDir.Dependencies.Count > 0)
                {
                    continue;
                }
                
                if (foundProjectDirInfo.Dependencies == null)
                {
                    foundProjectDirInfo.Dependencies = new HashSet<string>();
                }
                
                foreach (string dependency in pluginDir.Dependencies)
                {
                    foundProjectDirInfo.Dependencies.Add(dependency);
                }
                
                mergedModules[pluginDir.ProjectRoot] = foundProjectDirInfo;
            }
        }
        
        bool anyChanges = false;
        foreach (ProjectDirInfo pluginDir in mergedModules.Values)
        {
            CreateOrUpdateGlueModule(pluginDir.GlueCsProjPath, pluginDir.GlueProjectName, pluginDir.Dependencies, pluginDir.ProjectRoot, out bool createdNewModule);
            anyChanges |= createdNewModule;
        }
        
        // If the user doesn't have any C++ in their project, we need to create a Glue module for the project manually.
        // Used for runtime generated code such as GameplayTags.
        if (!hasProjectGlue)
        {
            UhtSession session = GeneratorStatics.Factory.Session;
            string projectRoot = session.ProjectDirectory!;
            string baseName = Path.GetFileNameWithoutExtension(session.ProjectFile!);
            string projectName = baseName + ".Glue";
            string csprojPath = Path.Join(projectRoot, "Script", projectName, projectName + ".csproj");

            CreateOrUpdateGlueModule(csprojPath, projectName, null, projectRoot, out bool createdNewModule);
            anyChanges |= createdNewModule;
        }

        if (anyChanges)
        {
            USharpBuildToolUtilities.InvokeUSharpBuildTool("GenerateSolution");
        }
        
        JsonUtilities.SerializeObjectToJson(_moduleDependencies!, ModuleDependenciesFileName);
    }

    private static void LoadModuleDependencies()
    {
        _moduleDependencies = JsonUtilities.DeserializeObjectFromJson<Dictionary<string, List<string>>>(ModuleDependenciesFileName);
        
        if (_moduleDependencies == null)
        {
            _moduleDependencies = new Dictionary<string, List<string>>();
        }
    }

    private static void CreateOrUpdateGlueModule(string csprojPath, string projectName, IEnumerable<string>? dependencyPaths, string projectRoot, out bool createdNewModule)
    {
        createdNewModule = false;
        
        if (!File.Exists(csprojPath))
        {
            string projectDirectory = Path.GetDirectoryName(csprojPath)!;
            List<KeyValuePair<string, string>> arguments = new List<KeyValuePair<string, string>>
            {
                new("NewProjectName", projectName),
                new("NewProjectFolder", Path.GetDirectoryName(projectDirectory)!),
                new("SkipIncludeProjectGlue", "true"),
                new("SkipSolutionGeneration", "true"),
                new("SkipUSharpProjSetup", "true"),
                new("ProjectRoot", projectRoot)
            };
            
            if (!USharpBuildToolUtilities.InvokeUSharpBuildTool("GenerateProject", arguments))
            {
                throw new InvalidOperationException($"Failed to create project file at {csprojPath}");
            }
            
            Console.WriteLine($"Successfully created project file: {projectName}");
            
            createdNewModule = true;
        }
        else
        {
            Console.WriteLine($"Project file already exists: {projectName}. Skipping creation.");
        }
        
        AddPluginDependencies(projectName, csprojPath, dependencyPaths, createdNewModule);
    }

    private static void AddPluginDependencies(string projectName, string projectPath, IEnumerable<string>? dependencyPaths, bool forceUpdate)
    {
        if (dependencyPaths == null)
        {
            return;
        }
        
        List<string> pluginDependencies = dependencyPaths.ToList();
        
        if (!forceUpdate && _moduleDependencies!.TryGetValue(projectName, out List<string>? existingDependencies))
        {
            if (existingDependencies.OrderBy(d => d).SequenceEqual(pluginDependencies.OrderBy(d => d)))
            {
                Console.WriteLine($"No changes in dependencies for project {projectName}. Skipping update.");
                return;
            }
        }
        
        _moduleDependencies![projectName] = pluginDependencies;
        
        List<KeyValuePair<string, string>> arguments = new()
        {
            new KeyValuePair<string, string>("ProjectPath", projectPath),
        };
        
        foreach (string path in pluginDependencies)
        {
            arguments.Add(new KeyValuePair<string, string>("Dependency", path));
        }

        if (!USharpBuildToolUtilities.InvokeUSharpBuildTool("UpdateProjectDependencies", arguments))
        {
            throw new InvalidOperationException($"Failed to update project dependencies for {projectPath}");
        }
        
        Console.WriteLine($"Updated project dependencies for {projectName}");
    }
}
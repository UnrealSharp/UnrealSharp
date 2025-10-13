using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using EpicGames.UHT.Utils;
using UnrealSharp.Shared;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator;

public static class USharpBuildToolUtilities
{
    public static void CreateGlueProjects()
    {
        bool hasProjectGlue = false;
        foreach (ProjectDirInfo pluginDir in PluginUtilities.PluginInfo.Values)
        {
            if (pluginDir.IsUProject)
            {
                hasProjectGlue = true;
            }
            
            TryCreateGlueProject(pluginDir.GlueCsProjPath, pluginDir.GlueProjectName, pluginDir.Dependencies, pluginDir.ProjectRoot);
        }

        if (!hasProjectGlue)
        {
            UhtSession session = Program.Factory.Session;
            string projectRoot = session.ProjectDirectory!;
            string baseName = Path.GetFileNameWithoutExtension(session.ProjectFile!);
            string projectName = baseName + ".Glue";
            string csprojPath = Path.Join(projectRoot, "Script", projectName, projectName + ".csproj");

            TryCreateGlueProject(csprojPath, projectName, null, projectRoot);
        }
    }

    public static void CreateBuildDirectoryFile() => InvokeUSharpBuildTool("EmitBuildProps");

    public static void TryCreateGlueProject(string csprojPath, string projectName, IEnumerable<string>? dependencyPaths, string projectRoot)
    {
        if (!File.Exists(csprojPath))
        {
            string projectDirectory = Path.GetDirectoryName(csprojPath)!;
            List<KeyValuePair<string, string>> arguments = new List<KeyValuePair<string, string>>
            {
                new("NewProjectName", projectName),
                new("NewProjectFolder", Path.GetDirectoryName(projectDirectory)!),
                new("SkipIncludeProjectGlue", "true"),
                new("SkipSolutionGeneration", "true"),
            };

            arguments.Add(new KeyValuePair<string, string>("ProjectRoot", projectRoot));
            if (!InvokeUSharpBuildTool("GenerateProject", arguments))
            {
                throw new InvalidOperationException($"Failed to create project file at {csprojPath}");
            }
            
            Console.WriteLine($"Successfully created project file: {projectName}");
        }
        else
        {
            Console.WriteLine($"Project file already exists: {projectName}. Skipping creation.");
        }
        
        AddPluginDependencies(projectName, csprojPath, dependencyPaths);
    }

    public static void AddPluginDependencies(string projectName, string projectPath, IEnumerable<string>? dependencies)
    {
        List<KeyValuePair<string, string>> arguments = new()
        {
            new KeyValuePair<string, string>("ProjectPath", projectPath),
        };

        if (dependencies != null)
        {
            foreach (string path in dependencies)
            {
                arguments.Add(new KeyValuePair<string, string>("Dependency", path));
            }
        }

        if (!InvokeUSharpBuildTool("UpdateProjectDependencies", arguments))
        {
            throw new InvalidOperationException($"Failed to update project dependencies for {projectPath}");
        }
        
        Console.WriteLine($"Updated project dependencies for {projectName}");
    }

    public static bool InvokeUSharpBuildTool(string action, List<KeyValuePair<string, string>>? arguments = null)
    {
        string path = Path.Combine(Program.ManagedBinariesPath, DotNetUtilities.DOTNET_MAJOR_VERSION_DISPLAY);
        return DotNetUtilities.InvokeUSharpBuildTool(action, path,
            Program.ProjectName,
            Program.PluginDirectory,
            Program.Factory.Session.ProjectDirectory!,
            Program.Factory.Session.EngineDirectory!,
            arguments);
    }
    
    public static void CompileUSharpBuildTool()
    {
        Console.WriteLine("Compiling USharpBuildTool...");
        
        string uSharpBuildToolDirectory = Path.Combine(Program.ManagedPath, "UnrealSharpPrograms");
        
        if (!Directory.Exists(uSharpBuildToolDirectory))
        {
            throw new DirectoryNotFoundException($"Failed to find UnrealSharpPrograms directory at: {uSharpBuildToolDirectory}");
        }
        
        Collection<string> arguments = new Collection<string>
        {
            "build",
        };
        
        if (!DotNetUtilities.InvokeDotNet(arguments, uSharpBuildToolDirectory))
        {
            throw new InvalidOperationException("Failed to compile USharpBuildTool.");
        }
    }
}
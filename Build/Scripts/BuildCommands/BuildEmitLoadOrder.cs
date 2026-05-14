using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutomationTool;
using UnrealBuildTool;
using UnrealSharp.Automation.Utilities;
using UnrealSharp.Shared;

namespace UnrealSharp.Automation.BuildCommands;

[Help("Builds the solution and emits a JSON file with the load order of the assemblies.")]
[Help("OutputPath=<Path>", "Optional output path for the build output.")]
[Help("clp=<Args>", "Optional CLP arguments to pass to the build process.")]
public class BuildEmitLoadOrder : BuildCommand
{
    private const string GlueProjectSuffix = ".Glue";
    private const string PropertiesFolderName = "Properties";
    private const string LaunchSettingsFileName = "launchSettings.json";

    public override void ExecuteBuild()
    {
        string OutputPath = ParseRequiredStringParam("OutputPath");

        List<string> ExtraArguments = new List<string>
        {
            $"-p:OutputPath=\"{OutputPath}\""
        };

        string[] Clp = ParseParamValues("clp");
        if (Clp.Length > 0)
        {
            ExtraArguments.Add($"-clp:{string.Join(';', Clp)}");
        }

        List<string> Folders = new List<string> { this.GetProjectScriptFolder() };
        BuildCommands.BuildSolution.RunBuild(Folders, UnrealTargetConfiguration.DebugGame, publish: true, ExtraArguments);

        string ResolvedOutputPath = PathUtilities.GetOutputPath(this.GetProjectRootFolder());
        EmitLoadOrder(this.GetUnrealSharpProjectFiles(), ResolvedOutputPath, ResolvedOutputPath);
        AddLaunchSettings(this);
    }
    
    public static void EmitLoadOrder(IReadOnlyList<FileInfo> projectFiles, string assemblyFolder, string publishPath)
    {
        LoggerUtilities.LogUnrealSharpInfo($"Emitting assembly load order for assemblies: {string.Join(", ", projectFiles.Select(file => Path.GetFileNameWithoutExtension(file.Name)))}");
        
        ArgumentNullException.ThrowIfNull(projectFiles);
        ArgumentException.ThrowIfNullOrEmpty(assemblyFolder);
        ArgumentException.ThrowIfNullOrEmpty(publishPath);

        if (projectFiles.Count == 0)
        {
            LoggerUtilities.LogUnrealSharpWarning("No project files found. Skipping assembly load order emission.");
            return;
        }

        if (!Directory.Exists(assemblyFolder))
        {
            throw new DirectoryNotFoundException($"Assembly folder does not exist: {assemblyFolder}");
        }

        List<string> AssemblyPaths = new List<string>(projectFiles.Count);
        foreach (FileInfo ProjectFile in projectFiles)
        {
            string CsProjName = Path.GetFileNameWithoutExtension(ProjectFile.Name);
            string AssemblyPath = Path.Combine(assemblyFolder, CsProjName + ".dll");

            if (!File.Exists(AssemblyPath))
            {
                LoggerUtilities.LogUnrealSharpWarning($"Could not find assembly for project {CsProjName} at expected path {AssemblyPath}. Skipping.");
                continue;
            }

            AssemblyPaths.Add(AssemblyPath);
        }

        if (AssemblyPaths.Count == 0)
        {
            LoggerUtilities.LogUnrealSharpWarning("No assemblies could be resolved for the supplied projects. Skipping load order emission.");
            return;
        }

        AssemblyUtilities.EmitLoadOrder(AssemblyPaths, publishPath);
    }

    private static void AddLaunchSettings(BuildCommand buildCommand)
    {
        ArgumentNullException.ThrowIfNull(buildCommand);

        List<FileInfo> AllProjectFiles = buildCommand.GetUnrealSharpProjectFiles();
        string ScriptFolder = buildCommand.GetProjectScriptFolder();

        foreach (FileInfo ProjectFile in AllProjectFiles)
        {
            DirectoryInfo? ProjectDirectory = ProjectFile.Directory;
            if (ProjectDirectory is null)
            {
                LoggerUtilities.LogUnrealSharpWarning($"Skipping launch-settings for {ProjectFile.FullName}: parent directory is null.");
                continue;
            }

            if (ProjectDirectory.Name.EndsWith(GlueProjectSuffix, StringComparison.Ordinal))
            {
                continue;
            }

            string CsProjectPath = Path.Combine(ScriptFolder, ProjectDirectory.Name);
            string PropertiesDirectoryPath = Path.Combine(CsProjectPath, PropertiesFolderName);
            string LaunchSettingsPath = Path.Combine(PropertiesDirectoryPath, LaunchSettingsFileName);

            if (!Directory.Exists(PropertiesDirectoryPath))
            {
                Directory.CreateDirectory(PropertiesDirectoryPath);
            }
            
            if (File.Exists(LaunchSettingsPath))
            {
                continue;
            }

            LaunchSettingsUtilities.CreateOrUpdateLaunchSettings(buildCommand, LaunchSettingsPath);
        }
    }
}
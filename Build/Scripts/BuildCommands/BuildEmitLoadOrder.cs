using System;
using System.Collections.Generic;
using System.IO;
using AutomationTool;
using Microsoft.Extensions.Logging;
using UnrealBuildTool;
using UnrealSharp.Automation.Processes;
using UnrealSharp.Automation.Utilities;

namespace UnrealSharp.Automation.BuildCommands;

[Help("Builds the solution and emits a JSON file with the load order of the assemblies.")]
[Help("OutputPath=<Path>", "Optional output path for the build output.")]
[Help("clp=<Args>", "Optional CLP arguments to pass to the build process.")]
public class BuildEmitLoadOrder : BuildCommand
{
    public override void ExecuteBuild()
    {
        string OutputPath = ParseRequiredStringParam("OutputPath");
        
        List<string> ExtraArguments = new List<string>();
        ExtraArguments.Add($"-p:OutputPath=\"{OutputPath}\"");

        string[] Clp = ParseParamValues("clp");
        string ClpJoined = Clp != null && Clp.Length > 0 ? string.Join(';', Clp) : string.Empty;
        
        if (!string.IsNullOrEmpty(ClpJoined))
        {
            ExtraArguments.Add($"-clp:{ClpJoined}");
        }

        UnrealTargetConfiguration BuildConfig = UnrealTargetConfiguration.DebugGame;
        List<string> Folders = new List<string> { this.GetProjectScriptFolder() };
        
        foreach (string SolutionFolder in Folders)
        {
            if (!Directory.Exists(SolutionFolder))
            {
                throw new Exception($"Specified solution folder does not exist: {SolutionFolder}");
            }

            DotnetProcess PublishProcess = new DotnetProcess();

            PublishProcess.StartInfo.ArgumentList.Add("publish");
            PublishProcess.StartInfo.ArgumentList.Add($"\"{SolutionFolder}\"");
            PublishProcess.StartInfo.ArgumentList.Add("--configuration");
            PublishProcess.StartInfo.ArgumentList.Add(BuildConfig.GetDotNetBuildConfiguration());

            foreach (string ExtraArgument in ExtraArguments)
            {
                PublishProcess.StartInfo.ArgumentList.Add(ExtraArgument);
            }

            PublishProcess.StartBuildToolProcess();
        }

        string ResolvedOutputPath = PathUtilities.GetOutputPath(this.GetProjectRootFolder());
        EmitLoadOrder(this.GetUnrealSharpProjectFiles(), ResolvedOutputPath, ResolvedOutputPath);
        AddLaunchSettings(this);
    }

    public static void EmitLoadOrder(List<FileInfo> projectFiles, string assemblyFolder, string publishPath)
    {
        if (projectFiles.Count == 0)
        {
            Logger.LogWarning("No project files found. Skipping assembly load order emission.");
            return;
        }

        List<string> AssemblyPaths = new List<string>(projectFiles.Count);
        foreach (FileInfo ProjectFile in projectFiles)
        {
            string CsProjName = Path.GetFileNameWithoutExtension(ProjectFile.Name);
            string AssemblyPath = Path.Combine(assemblyFolder, CsProjName + ".dll");

            if (!File.Exists(AssemblyPath))
            {
                Console.WriteLine($"Could not find assembly for project {CsProjName} at expected path {AssemblyPath}. Skipping.");
                continue;
            }

            AssemblyPaths.Add(AssemblyPath);
        }

        AssemblyUtilities.EmitLoadOrder(AssemblyPaths, publishPath);
    }

    private static void AddLaunchSettings(BuildCommand buildCommand)
    {
        List<FileInfo> AllProjectFiles = buildCommand.GetUnrealSharpProjectFiles();
        string ScriptFolder = buildCommand.GetProjectScriptFolder();

        foreach (FileInfo ProjectFile in AllProjectFiles)
        {
            if (ProjectFile.Directory!.Name.EndsWith(".Glue"))
            {
                continue;
            }

            string CsProjectPath = Path.Combine(ScriptFolder, ProjectFile.Directory.Name);
            string PropertiesDirectoryPath = Path.Combine(CsProjectPath, "Properties");
            string LaunchSettingsPath = Path.Combine(PropertiesDirectoryPath, "launchSettings.json");
            
            if (!Directory.Exists(PropertiesDirectoryPath))
            {
                Directory.CreateDirectory(PropertiesDirectoryPath);
            }
            
            if (File.Exists(LaunchSettingsPath))
            {
                return;
            }
            
            LaunchSettingsUtilities.CreateOrUpdateLaunchSettings(buildCommand, LaunchSettingsPath);
        }
    }
}

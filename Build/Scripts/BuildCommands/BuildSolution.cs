using System;
using System.Collections.Generic;
using System.IO;
using AutomationTool;
using UnrealBuildTool;
using UnrealSharp.Automation.Processes;
using UnrealSharp.Automation.Utilities;

namespace UnrealSharp.Automation.BuildCommands;

[Help("Builds a solution file using MSBuild. Can also publish the solution if the \"Publish\" parameter is set to true.")]
[Help("Folders=<Path>+<Path>", "One or more solution folders to build. At least one is required.")]
[Help("Publish", "If set, the solution is published instead of built.")]
[Help("BuildConfig=<Config>", "The build configuration (Debug, DebugGame, Development, Shipping, etc.).")]
[Help("ExtraArguments=<Arg>+<Arg>", "Additional arguments forwarded to dotnet build/publish.")]
public class BuildSolution : BuildCommand
{
    public override void ExecuteBuild()
    {
        string[] Folders = ParseParamValues("Folders");
        
        if (Folders.Length == 0)
        {
            throw new AutomationException("At least one solution folder must be specified via -Folders=<Path>.");
        }

        bool Publish = ParseParam("Publish");
        UnrealTargetConfiguration BuildConfig = ParseRequiredEnumParamEnum<UnrealTargetConfiguration>("BuildConfig");
        string[] ExtraArguments = ParseParamValues("ExtraArguments");

        RunBuild(Folders, BuildConfig, Publish, ExtraArguments);
    }
    
    public static void RunBuild(string folder, UnrealTargetConfiguration buildConfig, bool publish, IList<string>? extraArguments = null)
    {
        RunBuild([folder], buildConfig, publish, extraArguments);
    }
    
    public static void RunBuild(IEnumerable<string> folders, UnrealTargetConfiguration buildConfig, bool publish, IList<string>? extraArguments = null)
    {
        ArgumentNullException.ThrowIfNull(folders);

        List<string> FolderList = new List<string>(folders);
        
        if (FolderList.Count == 0)
        {
            throw new ArgumentException("At least one folder must be provided.", nameof(folders));
        }
        
        foreach (string SolutionFolder in FolderList)
        {
            if (string.IsNullOrWhiteSpace(SolutionFolder))
            {
                throw new ArgumentException("Solution folder paths cannot be null or empty.", nameof(folders));
            }

            if (!Directory.Exists(SolutionFolder))
            {
                throw new DirectoryNotFoundException($"Specified solution folder does not exist: {SolutionFolder}");
            }
        }

        string Action = publish ? "publish" : "build";
        string ConfigurationName = buildConfig.GetDotNetBuildConfiguration();

        foreach (string SolutionFolder in FolderList)
        {
            LoggerUtilities.LogUnrealSharpInfo($"Running dotnet {Action} on {SolutionFolder} (configuration: {ConfigurationName}).");

            using (DotnetProcess BuildSolutionProcess = new DotnetProcess())
            {
                BuildSolutionProcess.StartInfo.ArgumentList.Add(Action);
                BuildSolutionProcess.StartInfo.ArgumentList.Add(SolutionFolder);
                BuildSolutionProcess.StartInfo.ArgumentList.Add("--configuration");
                BuildSolutionProcess.StartInfo.ArgumentList.Add(ConfigurationName);

                if (extraArguments != null)
                {
                    foreach (string ExtraArgument in extraArguments)
                    {
                        BuildSolutionProcess.StartInfo.ArgumentList.Add(ExtraArgument);
                    }
                }

                BuildSolutionProcess.StartBuildToolProcess();
            }
        }
    }
}
using System;
using System.IO;
using AutomationTool;
using UnrealBuildTool;
using UnrealSharp.Automation.Processes;
using UnrealSharp.Automation.Utilities;

namespace UnrealSharp.Automation.BuildCommands;

[Help("Builds a solution file using MSBuild. Can also publish the solution if the \"Publish\" parameter is set to true.")]
public class BuildSolution : BuildCommand
{
    public override void ExecuteBuild()
    {
        string[] Folders = ParseParamValues("Folders");
        bool Publish = ParseParam("Publish");
        UnrealTargetConfiguration BuildConfig = ParseRequiredEnumParamEnum<UnrealTargetConfiguration>("BuildConfig");
        string[] ExtraArguments = ParseParamValues("ExtraArguments");
        
        foreach (string SolutionFolder in Folders)
        {
            if (!Directory.Exists(SolutionFolder))
            {
                throw new Exception($"Specified solution folder does not exist: {SolutionFolder}");
            }
        
            DotnetProcess BuildSolutionProcess = new DotnetProcess();

            BuildSolutionProcess.StartInfo.ArgumentList.Add(Publish ? "publish" : "build");
            BuildSolutionProcess.StartInfo.ArgumentList.Add($"\"{SolutionFolder}\"");
            BuildSolutionProcess.StartInfo.ArgumentList.Add("--configuration");
            BuildSolutionProcess.StartInfo.ArgumentList.Add(BuildConfig.GetDotNetBuildConfiguration());
        
            foreach (string ExtraArgument in ExtraArguments)
            {
                BuildSolutionProcess.StartInfo.ArgumentList.Add(ExtraArgument);
            }
        
            BuildSolutionProcess.StartBuildToolProcess();
        }
    }
}

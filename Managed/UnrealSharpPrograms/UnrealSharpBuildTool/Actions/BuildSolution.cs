using System.Collections.ObjectModel;
using CommandLine;

namespace UnrealSharpBuildTool.Actions;

public static class BuildSolutionAction
{
    public struct BuildSolutionParameters
    {
        [Option("BuildConfig", Required = true, HelpText = "The build configuration to use (Debug, Release, or Publish).")]
        public TargetConfiguration BuildConfig { get; set; }
        
        [Option("Folder", Required = true, HelpText = "The folder containing the solution file to build.")]
        public string Folder { get; set; }
        
        [Option("ExtraArguments", HelpText = "Additional arguments to pass to the build tool.")]
        public Collection<string> ExtraArguments { get; set; }
    }
    
    [Action("BuildSolution", "Builds the solution file in the specified folder.")]
    public static void BuildSolution(BuildSolutionParameters parameters)
    {
        if (!Directory.Exists(parameters.Folder))
        {
            throw new Exception($"Couldn't find the solution file at \"{parameters.Folder}\"");
        }
        
        BuildToolProcess buildSolutionProcess = new BuildToolProcess();
        
        if (parameters.BuildConfig == TargetConfiguration.Publish)
        {
            buildSolutionProcess.StartInfo.ArgumentList.Add("publish");
        }
        else
        {
            buildSolutionProcess.StartInfo.ArgumentList.Add("build");
        }
        
        buildSolutionProcess.StartInfo.ArgumentList.Add($"{parameters.Folder}");
        
        buildSolutionProcess.StartInfo.ArgumentList.Add("--configuration");
        buildSolutionProcess.StartInfo.ArgumentList.Add(parameters.BuildConfig.ToString().ToLowerInvariant());
        
        buildSolutionProcess.StartBuildToolProcess();
    }
}
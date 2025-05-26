using System.Collections.ObjectModel;
using CommandLine;

namespace UnrealSharpBuildTool.Actions;

public static class BuildSolutionAction
{
    public struct BuildSolutionParameters
    {
        [Option("BuildConfig", Required = true, HelpText = "The build configuration to use (Debug, Release, or Publish).")]
        public TargetConfiguration BuildConfig { get; set; }
        
        [Option("Publish", Required = false, Default = false, HelpText = "If true, the solution will be published instead of built. Defaults to false.")]
        public bool Publish { get; set; }
        
        [Option("Folders", Required = true, HelpText = "The folder containing the solution files to build.")]
        public List<string> Folders { get; set; }
        
        [Option("ExtraArguments", HelpText = "Additional arguments to pass to the build tool.")]
        public Collection<string> ExtraArguments { get; set; }
    }
    
    [Action("BuildSolution", "Builds the solution file in the specified folder.")]
    public static void BuildSolution(BuildSolutionParameters parameters)
    {
        foreach (string folder in parameters.Folders)
        {
            if (!Directory.Exists(folder))
            {
                throw new Exception($"Couldn't find the solution file at \"{folder}\"");
            }
        
            BuildToolProcess buildSolutionProcess = new BuildToolProcess();
        
            if (parameters.Publish)
            {
                buildSolutionProcess.StartInfo.ArgumentList.Add("publish");
            }
            else
            {
                buildSolutionProcess.StartInfo.ArgumentList.Add("build");
            }
        
            buildSolutionProcess.StartInfo.ArgumentList.Add($"\"{folder}\"");
        
            buildSolutionProcess.StartInfo.ArgumentList.Add("--configuration");
            buildSolutionProcess.StartInfo.ArgumentList.Add(parameters.BuildConfig.ToString().ToLowerInvariant());
        
            foreach (string extraArgument in parameters.ExtraArguments)
            {
                buildSolutionProcess.StartInfo.ArgumentList.Add(extraArgument);
            }
        
            buildSolutionProcess.StartBuildToolProcess();
        }
    }
}
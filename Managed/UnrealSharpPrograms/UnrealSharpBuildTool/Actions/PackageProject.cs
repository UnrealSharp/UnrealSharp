using System.Collections.ObjectModel;
using CommandLine;

namespace UnrealSharpBuildTool.Actions;

public static class PackageProjectAction
{
    public struct PackageProjectParameters
    {
        [Option("ArchiveDirectory", Required = true, HelpText = "The directory base directory where the packaged game is located")]
        public string ArchiveDirectory { get; set; }
        
        [Option("TargetPlatform", Required = true, HelpText = "The target platform for the package")]
        public TargetPlatform TargetPlatform { get; set; }
        
        [Option("TargetArchitecture", Required = true, HelpText = "The target architecture for the package")]
        public TargetArchitecture TargetArchitecture { get; set; }
         
        [Option("BuildConfig", Required = true, HelpText = "The build configuration for the package (Debug, Release, etc.)")]
        public TargetConfiguration BuildConfig { get; set; }
        
        [Option("NativeAOT", HelpText = "Enable Native AOT compilation. Will overwrite the BuildConfig to Release if set to true.")]
        public bool NativeAOT { get; set; }
    }
    
    [Action("PackageProject", "Packages the project for distribution.")]
    public static void PackageProject(PackageProjectParameters parameters)
    {
        if (string.IsNullOrEmpty(parameters.ArchiveDirectory))
        {
            throw new Exception("ArchiveDirectory argument is required for the Publish action.");
        }

        string rootProjectPath = Path.Combine(parameters.ArchiveDirectory, Program.BuildToolOptions.ProjectName);
        string binariesPath = Program.GetOutputPath(rootProjectPath);
        string bindingsPath = Path.Combine(Program.BuildToolOptions.PluginDirectory, "Managed", "UnrealSharp");
        
        Collection<string> extraArguments =
        [
            "--self-contained",
            "--runtime",
            "win-x64",
			"-p:DefineAdditionalConstants=PACKAGE",
            $"-p:PublishDir=\"{binariesPath}\""
        ];

        BuildSolutionAction.BuildSolutionParameters buildBindingsLibrary = new BuildSolutionAction.BuildSolutionParameters
        {
            ExtraArguments = extraArguments,
            BuildConfig = TargetConfiguration.Publish,
            Folder = bindingsPath
        };
        BuildSolutionAction.BuildSolution(buildBindingsLibrary);
        
        BuildSolutionAction.BuildSolutionParameters buildUserSolution = new BuildSolutionAction.BuildSolutionParameters
        {
            ExtraArguments = extraArguments,
            BuildConfig = TargetConfiguration.Publish,
            Folder = Program.GetScriptFolder()
        };
        BuildSolutionAction.BuildSolution(buildUserSolution);
        
        Weaving.WeaveParameters weaveParameters = new Weaving.WeaveParameters
        {
            OutputDirectory = binariesPath,
            BuildConfig = parameters.BuildConfig,
        };
        
        Weaving.WeaveProject(weaveParameters);

        if (parameters.NativeAOT)
        {
            PublishAsAOT(parameters);
        }
    }

    static void PublishAsAOT(PackageProjectParameters parameters)
    {

    }
}

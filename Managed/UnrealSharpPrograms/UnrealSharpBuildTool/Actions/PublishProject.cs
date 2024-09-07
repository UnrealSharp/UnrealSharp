using System.Collections.ObjectModel;

namespace UnrealSharpBuildTool.Actions;

public class PublishProject : BuildToolAction
{
    public override bool RunAction()
    {
        // Force the build configuration to be Publish, for now.
        // I'm gonna rewrite this later anyways.
        Program.BuildToolOptions.BuildConfig = BuildConfig.Publish;
        
        string archiveDirectoryPath = Program.TryGetArgument("ArchiveDirectory");
        
        if (string.IsNullOrEmpty(archiveDirectoryPath))
        {
            throw new Exception("ArchiveDirectory argument is required for the Publish action.");
        }
        
        string binariesPath = Program.GetOutputPath(archiveDirectoryPath);
        string bindingsPath = Path.Combine(Program.BuildToolOptions.PluginDirectory, "Managed", "UnrealSharp");
        
        Collection<string> extraArguments =
        [
            "--self-contained",
            "--runtime",
            "win-x64",
            $"-p:PublishDir=\"{binariesPath}\""
        ];

        BuildSolution.StartBuildingSolution(bindingsPath, Program.BuildToolOptions.BuildConfig, extraArguments);
        
        BuildSolution buildSolution = new BuildSolution();
        buildSolution.RunAction();
        
        WeaveProject weaveProject = new WeaveProject();
        weaveProject.RunAction();
        
        return true;
    }
}
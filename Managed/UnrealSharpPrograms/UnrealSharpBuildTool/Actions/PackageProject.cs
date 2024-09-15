using System.Collections.ObjectModel;

namespace UnrealSharpBuildTool.Actions;

public class PackageProject : BuildToolAction
{
    public override bool RunAction()
    {
        string archiveDirectoryPath = Program.TryGetArgument("ArchiveDirectory");
        
        if (string.IsNullOrEmpty(archiveDirectoryPath))
        {
            throw new Exception("ArchiveDirectory argument is required for the Publish action.");
        }

        string rootProjectPath = Path.Combine(archiveDirectoryPath, Program.BuildToolOptions.ProjectName);
        string binariesPath = Program.GetOutputPath(rootProjectPath);
        string bindingsPath = Path.Combine(Program.BuildToolOptions.PluginDirectory, "Managed", "UnrealSharp");
        
        Collection<string> extraArguments =
        [
            "--self-contained",
            "--runtime",
            "win-x64",
            $"-p:PublishDir=\"{binariesPath}\""
        ];

        BuildSolution.StartBuildingSolution(bindingsPath, BuildConfig.Publish, extraArguments);
        
        BuildSolution buildSolution = new BuildSolution(BuildConfig.Publish);
        buildSolution.RunAction();
        
        WeaveProject weaveProject = new WeaveProject(binariesPath);
        weaveProject.RunAction();
        
        return true;
    }
}
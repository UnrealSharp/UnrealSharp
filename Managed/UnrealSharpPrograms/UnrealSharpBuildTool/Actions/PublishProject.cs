using System.Collections.ObjectModel;

namespace UnrealSharpBuildTool.Actions;

public class PublishProject : BuildToolAction
{
    public override bool RunAction()
    {
        // Force the build configuration to be Publish, for now.
        // I'm gonna rewrite this later anyways.
        Program.buildToolOptions.BuildConfig = BuildConfig.Publish;
        
        string bindingsPath = Path.Combine(Program.buildToolOptions.PluginDirectory, "Managed", "UnrealSharp");
        
        Collection<string> extraArguments =
        [
            "--self-contained",
            "--runtime",
            "win-x64",
            $"-p:PublishDir=\"{Program.GetOutputPath()}\""
        ];

        BuildSolution.StartBuildingSolution(bindingsPath, Program.buildToolOptions.BuildConfig, extraArguments);
        
        BuildSolution buildSolution = new BuildSolution();
        buildSolution.RunAction();
        
        WeaveProject weaveProject = new WeaveProject();
        weaveProject.RunAction();
        
        return true;
    }
}
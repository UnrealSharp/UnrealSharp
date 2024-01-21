using System.Collections.ObjectModel;

namespace UnrealSharpBuildTool.Actions;

public class BuildSolution : BuildToolAction
{
    public override bool RunAction()
    {
        return StartBuildingSolution(Program.GetScriptFolder(), Program.buildToolOptions.BuildConfig);
    }

    private static bool StartBuildingSolution(string slnPath, BuildConfig buildConfig)
    {
        slnPath = Program.FixPath(slnPath);
        
        if (!Directory.Exists(slnPath))
        {
            throw new Exception($"Couldn't find the solution file at \"{slnPath}\"");
        }
        
        BuildToolProcess buildSolutionProcess = new BuildToolProcess();
        
        // Add the build command.
        buildSolutionProcess.StartInfo.ArgumentList.Add("build");
        buildSolutionProcess.StartInfo.ArgumentList.Add($"\"{slnPath}\"");
        
        // Add the build configuration.
        buildSolutionProcess.StartInfo.ArgumentList.Add("--configuration");
        buildSolutionProcess.StartInfo.ArgumentList.Add(buildConfig.ToString());

        return buildSolutionProcess.StartBuildToolProcess();
    }
}
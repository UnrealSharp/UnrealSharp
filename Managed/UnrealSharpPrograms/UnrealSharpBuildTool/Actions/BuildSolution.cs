using System.Collections.ObjectModel;

namespace UnrealSharpBuildTool.Actions;

public class BuildSolution() : BuildToolAction
{
    public override bool RunAction()
    {
        return StartBuildingSolution(Program.GetScriptFolder(), Program.buildToolOptions.BuildConfig);
    }

    public static bool StartBuildingSolution(string slnPath, BuildConfig buildConfig, Collection<string>? extraArguments = null)
    {
        slnPath = Program.FixPath(slnPath);
        
        if (!Directory.Exists(slnPath))
        {
            throw new Exception($"Couldn't find the solution file at \"{slnPath}\"");
        }
        
        BuildToolProcess buildSolutionProcess = new BuildToolProcess();
        
        if (buildConfig == BuildConfig.Publish)
        {
            buildSolutionProcess.StartInfo.ArgumentList.Add("publish");
        }
        else
        {
            buildSolutionProcess.StartInfo.ArgumentList.Add("build");
        }
        
        buildSolutionProcess.StartInfo.ArgumentList.Add($"\"{slnPath}\"");
        
        buildSolutionProcess.StartInfo.ArgumentList.Add("--configuration");
        buildSolutionProcess.StartInfo.ArgumentList.Add(Program.GetBuildConfiguration(buildConfig));
        
        if (extraArguments != null)
        {
            foreach (var argument in extraArguments)
            {
                buildSolutionProcess.StartInfo.ArgumentList.Add(argument);
            }
        }

        return buildSolutionProcess.StartBuildToolProcess();
    }
}
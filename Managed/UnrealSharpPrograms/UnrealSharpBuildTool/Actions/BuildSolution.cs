using System.Collections.ObjectModel;

namespace UnrealSharpBuildTool.Actions;

public class BuildSolution : BuildToolAction
{
    public override bool RunAction()
    {
        return StartBuildingSolution(Program.GetScriptFolder(), Program.buildToolOptions.BuildConfig);
    }
    
    private static string GetBuildConfiguration(BuildConfig buildConfig)
    {
        return buildConfig switch
        {
            BuildConfig.Debug => "Debug",
            BuildConfig.Release => "Release",
            BuildConfig.Publish => "Release",
            _ => "Release"
        };
    }

    private static bool StartBuildingSolution(string slnPath, BuildConfig buildConfig)
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
        buildSolutionProcess.StartInfo.ArgumentList.Add(GetBuildConfiguration(buildConfig));

        if (buildConfig == BuildConfig.Publish)
        {
            buildSolutionProcess.StartInfo.ArgumentList.Add("--self-contained");
            buildSolutionProcess.StartInfo.ArgumentList.Add("true");
            
            buildSolutionProcess.StartInfo.ArgumentList.Add("--runtime");
            buildSolutionProcess.StartInfo.ArgumentList.Add("win-x64");
        }

        return buildSolutionProcess.StartBuildToolProcess();
    }
}
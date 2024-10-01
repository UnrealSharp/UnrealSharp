using System.Collections.ObjectModel;

namespace UnrealSharpBuildTool.Actions;

public class RebuildSolution : BuildToolAction
{
    public override bool RunAction()
    {
        CleanSolution cleanSolutionProcess = new CleanSolution();
        
        if (!cleanSolutionProcess.RunAction())
        {
            return false;
        }

        BuildSolution buildSolution = new BuildUserSolution();
        
        if (!buildSolution.RunAction())
        {
            return false;
        }

        return true;
    }
}
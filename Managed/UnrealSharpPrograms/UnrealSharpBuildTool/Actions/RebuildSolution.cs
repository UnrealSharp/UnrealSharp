namespace UnrealSharpBuildTool.Actions;

public class RebuildSolution : BuildSolution
{
    public override bool RunAction()
    {
        CleanSolution cleanSolutionProcess = new CleanSolution();
        
        if (!cleanSolutionProcess.RunAction())
        {
            return false;
        }

        BuildSolution buildSolution = new BuildSolution();
        
        if (!buildSolution.RunAction())
        {
            return false;
        }

        return true;
    }
}
namespace UnrealSharpBuildTool.Actions;

public class BuildWeave : BuildToolAction
{
    public override bool RunAction()
    {
        BuildSolution buildSolution = new BuildSolution();
        WeaveProject weaveProject = new WeaveProject();
        return buildSolution.RunAction() && weaveProject.RunAction();
    }
}
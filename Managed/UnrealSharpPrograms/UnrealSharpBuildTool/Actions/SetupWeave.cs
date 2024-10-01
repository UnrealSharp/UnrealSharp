namespace UnrealSharpBuildTool.Actions;

public class SetupWeave : BuildToolAction
{
    public override bool RunAction()
    {
        BuildSolution buildSolution = new BuildUserSolution();
        WeaveProject weaveProject = new WeaveProject(setup: true);
        return buildSolution.RunAction() && weaveProject.RunAction();
    }
}
namespace UnrealSharpBuildTool.Actions;

public class PackageProject : BuildToolAction
{
    public override bool RunAction()
    {
        BuildSolution buildSolution = new BuildSolution();

        if (!buildSolution.RunAction())
        {
            throw new Exception("Failed to build the solution.");
        }
        
        WeaveProject weaveProject = new WeaveProject();
        if (!weaveProject.RunAction())
        {
            throw new Exception("Failed to weave the project.");
        }

        return true;
    }
}
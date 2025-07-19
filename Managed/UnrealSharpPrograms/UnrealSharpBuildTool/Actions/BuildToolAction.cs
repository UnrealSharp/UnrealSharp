namespace UnrealSharpBuildTool.Actions;

public abstract class BuildToolAction
{
    public static bool InitializeAction()
    {
        BuildToolAction buildToolAction = Program.BuildToolOptions.Action switch
        {
            BuildAction.Build => new BuildUserSolution(),
            BuildAction.Clean => new CleanSolution(),
            BuildAction.GenerateProject => new GenerateProject(),
            BuildAction.UpdateProjectDependencies => new UpdateProjectDependencies(),
            BuildAction.Rebuild => new RebuildSolution(),
            BuildAction.Weave => new WeaveProject(),
            BuildAction.PackageProject => new PackageProject(),
            BuildAction.GenerateSolution => new GenerateSolution(),
            BuildAction.UpdateProjectSolution => new UpdateProjectSolution(),
            BuildAction.BuildWeave => new BuildWeave(),
            _ => throw new Exception($"Can't find build action with name \"{Program.BuildToolOptions.Action}\"")
        };

        return buildToolAction.RunAction();
    }

    public abstract bool RunAction();
}

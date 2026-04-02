namespace UnrealSharpBuildTool.Actions;

public abstract class BuildToolAction
{
    public static bool InitializeAction()
    {
        BuildToolAction buildToolAction = Program.BuildToolOptions.Action switch
        {
            BuildAction.GenerateProject => new GenerateProject(),
            BuildAction.UpdateProjectDependencies => new UpdateProjectDependencies(),
            BuildAction.PackageProject => new PackageProject(),
            BuildAction.GenerateSolution => new GenerateSolution(),
            BuildAction.BuildEmitLoadOrder => new BuildEmitLoadOrder(),
            _ => throw new Exception($"Can't find build action with name \"{Program.BuildToolOptions.Action}\"")
        };

        return buildToolAction.RunAction();
    }

    public abstract bool RunAction();
}

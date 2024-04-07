namespace UnrealSharpBuildTool.Actions;

public abstract class BuildToolAction
{ 
    public static bool InitializeAction()
    {
        BuildToolAction buildToolAction = Program.buildToolOptions.Action switch
        {
            BuildAction.Build => new BuildSolution(),
            BuildAction.Clean => new CleanSolution(),
            BuildAction.GenerateProject => new GenerateProject(),
            BuildAction.Rebuild => new RebuildSolution(),
            BuildAction.Weave => new WeaveProject(),
            BuildAction.Publish => new PublishProject(),
            _ => throw new Exception($"Can't find build action with name \"{Program.buildToolOptions.Action}\"")
        };

        return buildToolAction.RunAction();
    }

    public abstract bool RunAction();
}
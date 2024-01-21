namespace UnrealSharpBuildTool.Actions;

public class CleanSolution : BuildToolAction
{
    public override bool RunAction()
    {
        BuildToolProcess cleanProcess = new BuildToolProcess();

        string unrealSharpBinaries = Program.GetOutputPath();

        if (Directory.Exists(unrealSharpBinaries))
        {
            Directory.Delete(unrealSharpBinaries, true);
        }
        
        return cleanProcess.StartBuildToolProcess();
    }
}
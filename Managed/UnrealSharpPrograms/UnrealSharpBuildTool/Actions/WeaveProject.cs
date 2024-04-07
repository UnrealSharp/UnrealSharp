namespace UnrealSharpBuildTool.Actions;

public class WeaveProject : BuildToolAction
{
    public override bool RunAction()
    {
        var weaverPath = Program.GetWeaver();
        
        if (!File.Exists(weaverPath))
        {
            throw new Exception("Couldn't find the weaver");
        }

        var scriptFolderBinaries = Program.GetScriptFolderBinaries();
        var outputPath = Program.GetOutputPath();
        var projectName = Program.GetProjectNameAsManaged();

        BuildToolProcess weaveProcess = new BuildToolProcess();
        
        // Add path to the compiled binaries.
        weaveProcess.StartInfo.ArgumentList.Add(weaverPath);
        
        weaveProcess.StartInfo.ArgumentList.Add("-p");
        weaveProcess.StartInfo.ArgumentList.Add($"\"{Program.FixPath(scriptFolderBinaries)}\"");

        // Add path to the output folder for the weaver.
        weaveProcess.StartInfo.ArgumentList.Add("-o");
        weaveProcess.StartInfo.ArgumentList.Add($"\"{Program.FixPath(outputPath)}\"");

        // Add the project name.
        weaveProcess.StartInfo.ArgumentList.Add("-n");
        weaveProcess.StartInfo.ArgumentList.Add(projectName);
        
        return weaveProcess.StartBuildToolProcess();
    }
}
namespace UnrealSharpBuildTool.Actions;

public class WeaveProject : BuildToolAction
{
    readonly string _outputDirectory;

    public WeaveProject(string outputDirectory = "")
    {
        _outputDirectory = string.IsNullOrEmpty(outputDirectory) ? Program.GetOutputPath() : outputDirectory;
    }

    public override bool RunAction()
    {
        string weaverPath = Program.GetWeaver();

        if (!File.Exists(weaverPath))
        {
            throw new Exception("Couldn't find the weaver");
        }

        DirectoryInfo scriptRootDirInfo = new DirectoryInfo(Program.GetProjectDirectory());
        return Weave(scriptRootDirInfo, weaverPath);
    }

    private bool Weave(DirectoryInfo scriptFolder, string weaverPath)
    {
        var projectFiles = Program.GetProjectFilesByDirectory(scriptFolder);
        var allProjectFiles = projectFiles.Values.SelectMany(x => x).ToList();
        if (allProjectFiles.Count == 0)
        {
            Console.WriteLine("No project files found. Skipping weaving...");
            return true;
        }

        using BuildToolProcess weaveProcess = new BuildToolProcess();
        weaveProcess.StartInfo.ArgumentList.Add(weaverPath);
        weaveProcess.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();

        bool foundValidProject = false;
        foreach (FileInfo projectFile in allProjectFiles)
        {
            weaveProcess.StartInfo.ArgumentList.Add("-p");
            string csProjName = Path.GetFileNameWithoutExtension(projectFile.Name);
            string assemblyPath = Path.Combine(projectFile.DirectoryName!, "bin",
                Program.GetBuildConfiguration(), Program.GetVersion(), csProjName + ".dll");

            weaveProcess.StartInfo.ArgumentList.Add(assemblyPath);
            foundValidProject = true;
        }

        if (!foundValidProject)
        {
            Console.WriteLine("No valid project found to weave. Skipping weaving...");
            return true;
        }

        // Add path to the output folder for the weaver.
        weaveProcess.StartInfo.ArgumentList.Add("-o");
        weaveProcess.StartInfo.ArgumentList.Add($"{Program.FixPath(_outputDirectory)}");

        return weaveProcess.StartBuildToolProcess();
    }
}

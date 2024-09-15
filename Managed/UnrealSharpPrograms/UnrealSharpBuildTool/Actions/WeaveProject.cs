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

        DirectoryInfo scriptRootDirInfo = new DirectoryInfo(Program.GetScriptFolder());
        return Weave(scriptRootDirInfo, weaverPath);
    }
    
    private bool Weave(DirectoryInfo scriptFolder, string weaverPath)
    {
        FileInfo[] csprojFiles = scriptFolder.GetFiles("*.csproj", SearchOption.AllDirectories);
        FileInfo[] fsprojFiles = scriptFolder.GetFiles("*.fsproj", SearchOption.AllDirectories);
        
        if (csprojFiles.Length == 0 && fsprojFiles.Length == 0)
        {
            return false;
        }
        
        List<FileInfo> allProjectFiles = new List<FileInfo>(csprojFiles.Length + fsprojFiles.Length);
        allProjectFiles.AddRange(csprojFiles);
        allProjectFiles.AddRange(fsprojFiles);
        
        BuildToolProcess weaveProcess = new BuildToolProcess();
        weaveProcess.StartInfo.ArgumentList.Add(weaverPath);
        weaveProcess.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
        
        foreach (FileInfo projectFile in allProjectFiles)
        {
            if (projectFile.Directory.Name == "ProjectGlue")
            {
                continue;
            }
            
            weaveProcess.StartInfo.ArgumentList.Add("-p");
            string csProjName = Path.GetFileNameWithoutExtension(projectFile.Name);
            string assemblyPath = Path.Combine(projectFile.DirectoryName!, "bin", 
                Program.GetBuildConfiguration(), Program.GetVersion(), csProjName + ".dll");
            
            weaveProcess.StartInfo.ArgumentList.Add(assemblyPath);
        }

        // Add path to the output folder for the weaver.
        weaveProcess.StartInfo.ArgumentList.Add("-o");
        weaveProcess.StartInfo.ArgumentList.Add($"{Program.FixPath(_outputDirectory)}");
        
        return weaveProcess.StartBuildToolProcess();
    }
}
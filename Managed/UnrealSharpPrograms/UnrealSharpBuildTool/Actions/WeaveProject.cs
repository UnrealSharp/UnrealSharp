using CommandLine;

namespace UnrealSharpBuildTool.Actions;

public static class Weaving
{
    public struct WeaveParameters
    {
        [Option('o', "outputDirectory", Required = false, HelpText = "Output directory for the weaved files. Will default to ProjectRoot/Binaries/Managed if not specified.")]
        public string OutputDirectory { get; set; }
        
        [Option("weaverPath", Required = false, HelpText = "Path to the weaver tool. If not specified, the default path will be used.")]
        public TargetConfiguration BuildConfig { get; set; }
        
        [Option("AssemblyPaths", Required = false, HelpText = "List of assembly paths to weave. If not specified, all assemblies in the project will be used.")]
        public IEnumerable<string> AssemblyPaths { get; set; }
        
        [Option("CopyDependencies", Required = false, Default = true, HelpText = "If true, copies dependencies to the output directory. Defaults to true.")]
        public bool CopyDependencies { get; set; }
    }
    
    [Action("WeaveProject", "Weaves the project files with the weaver tool.")]
    public static void WeaveProject(WeaveParameters parameters)
    {
        string outputPath = string.IsNullOrEmpty(parameters.OutputDirectory) ? Program.GetOutputPath() : parameters.OutputDirectory;
        string weaverPath = Program.GetWeaver();
        
        if (!File.Exists(weaverPath))
        {
            throw new Exception("Couldn't find the weaver");
        }

        DirectoryInfo scriptRootDirInfo = new DirectoryInfo(Program.GetScriptFolder());
        List<FileInfo> allProjectFiles = Program.GetAllProjectFiles(scriptRootDirInfo);
        if (allProjectFiles.Count == 0)
        {
            Console.WriteLine("No project files found. Skipping weaving...");
            return;
        }
        
        BuildToolProcess weaveProcess = new BuildToolProcess();
        weaveProcess.StartInfo.ArgumentList.Add(weaverPath);
        weaveProcess.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
        
        bool foundValidProject = false;
        if (parameters.AssemblyPaths.Any())
        {
            foreach (string assemblyPath in parameters.AssemblyPaths)
            {
                if (!File.Exists(assemblyPath))
                {
                    throw new Exception($"Assembly path '{assemblyPath}' does not exist.");
                }
                
                weaveProcess.StartInfo.ArgumentList.Add("-p");
                weaveProcess.StartInfo.ArgumentList.Add(assemblyPath);
                foundValidProject = true;
            }
        }
        else
        {
            foreach (FileInfo projectFile in allProjectFiles)
            {
                weaveProcess.StartInfo.ArgumentList.Add("-p");
                string csProjName = Path.GetFileNameWithoutExtension(projectFile.Name);
                string assemblyPath = Path.Combine(projectFile.DirectoryName!, "bin", 
                    parameters.BuildConfig.ToString(), Program.GetVersion(), csProjName + ".dll");
            
                weaveProcess.StartInfo.ArgumentList.Add(assemblyPath);
                foundValidProject = true;
            } 
        }
        
        if (!foundValidProject)
        {
            Console.WriteLine("No valid project found to weave. Skipping weaving...");
            return;
        }

        // Add path to the output folder for the weaver.
        weaveProcess.StartInfo.ArgumentList.Add("-o");
        weaveProcess.StartInfo.ArgumentList.Add($"{Program.FixPath(outputPath)}");
        
        if (parameters.CopyDependencies)
        {
            weaveProcess.StartInfo.ArgumentList.Add("--copy-dependencies");
        }
        
        weaveProcess.StartBuildToolProcess();
    }
}
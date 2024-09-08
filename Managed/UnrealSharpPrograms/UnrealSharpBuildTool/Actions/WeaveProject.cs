using Microsoft.VisualBasic.CompilerServices;

namespace UnrealSharpBuildTool.Actions;

public class WeaveProject : BuildToolAction
{
    private bool recursiveWeave(DirectoryInfo currentDir, string outputPath)
    {
        Console.WriteLine("Weave dir: "+currentDir.FullName);
        var weaverPath = Program.GetWeaver();
        
        if (!File.Exists(weaverPath))
        {
            throw new Exception("Couldn't find the weaver");
        }

        bool retval = true;
        foreach (var dir in currentDir.GetDirectories())
        {
            if (!recursiveWeave(dir, outputPath))
            {
                retval = false;
            }
        }

        var csprojFiles = currentDir.GetFiles("*.csproj");
        var fsprojFiles = currentDir.GetFiles("*.fsproj");
        var projFiles = csprojFiles.Concat(fsprojFiles).ToArray();
        if (projFiles.Length > 0)
        {
            Console.WriteLine("Weaving "+projFiles[0].FullName);
            var projectName = projFiles[0].Name.Replace(".csproj", "").Replace(".fsproj", "");
            BuildToolProcess weaveProcess = new BuildToolProcess();
        
            // Add path to the compiled binaries.
            weaveProcess.StartInfo.ArgumentList.Add(weaverPath);
            
            var scriptFolderBinaries = Path.Combine(currentDir.FullName, "bin", 
                Program.GetBuildConfiguration(), Program.GetVersion());
        
            weaveProcess.StartInfo.ArgumentList.Add("-p");
            weaveProcess.StartInfo.ArgumentList.Add($"{Program.FixPath(scriptFolderBinaries)}");

            // Add path to the output folder for the weaver.
            weaveProcess.StartInfo.ArgumentList.Add("-o");
            weaveProcess.StartInfo.ArgumentList.Add($"{Program.FixPath(outputPath)}");

            // Add the project name.
            weaveProcess.StartInfo.ArgumentList.Add("-n");
            weaveProcess.StartInfo.ArgumentList.Add(projectName);
        
            retval = weaveProcess.StartBuildToolProcess();
        }

        return retval;
    }
    public override bool RunAction()
    {
        var weaverPath = Program.GetWeaver();
        
        if (!File.Exists(weaverPath))
        {
            throw new Exception("Couldn't find the weaver");
        }

        var scriptRootDirInfo = new DirectoryInfo(Program.GetScriptFolder());
        var outputPath = Program.GetOutputPath();
        
        
        return recursiveWeave(scriptRootDirInfo, outputPath);
    }
}
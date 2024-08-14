using System.Diagnostics;
using Microsoft.VisualBasic.CompilerServices;

namespace UnrealSharpBuildTool.Actions;

public class WeaveProject : BuildToolAction
{
    private bool recursiveWeave(DirectoryInfo currentDir, string outputPath)
    {
        var weaverPath = Program.GetWeaver();
        
        if (!File.Exists(weaverPath))
        {
            throw new Exception("Couldn't find the weaver");
        }
        
        foreach (var dir in currentDir.GetDirectories())
        {
            if (!recursiveWeave(dir, outputPath))
            {
                return false;
            }
        }

        var projFiles = currentDir.GetFiles("*.csproj");
        //Console.WriteLine(projFiles.Length);
        if (projFiles.Length > 0)
        {
            var projectName = projFiles[0].Name.Replace(".csproj", "");
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
        
            return weaveProcess.StartBuildToolProcess();
        }

        return true;
    }
    public override bool RunAction()
    {
        //Console.WriteLine(" >>> weave");
        try
        {
            var scriptRootDirInfo = new DirectoryInfo(Program.GetScriptFolder());
            var outputPath = Program.GetOutputPath();
           // Console.WriteLine("<<< weave");
            return recursiveWeave(scriptRootDirInfo, outputPath);
  
        } catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
            return false;
        }
    }
}
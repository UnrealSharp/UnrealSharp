using System.Diagnostics;

namespace UnrealSharpBuildTool;

public class BuildToolProcess : Process
{
    public BuildToolProcess(string? fileName = null)
    {
        if (fileName == null)
        {
            fileName = Program.buildToolOptions.DotNetExecutable ?? "dotnet";
        }
        
        StartInfo.FileName = fileName;
        StartInfo.RedirectStandardOutput = true;
        StartInfo.RedirectStandardError = true;
        StartInfo.UseShellExecute = false;
        StartInfo.CreateNoWindow = true;
    }

    public bool StartBuildToolProcess()
    {
        try
        {
            if (!Start())
            {
                throw new Exception("Failed to start process");
            }
            
            string output = StandardOutput.ReadToEnd();
            string error = StandardError.ReadToEnd();
            
            WaitForExit();

            if (ExitCode != 0)
            {
                throw new Exception($"Error in executing build command {StartInfo.Arguments}: {Environment.NewLine + error + Environment.NewLine + output}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            return false;
        }

        return true;
    }
}
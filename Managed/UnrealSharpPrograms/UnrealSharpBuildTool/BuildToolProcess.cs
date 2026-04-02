using System.Diagnostics;
using System.Text;

namespace UnrealSharpBuildTool;

public class BuildToolProcess : Process
{
    public BuildToolProcess(string? fileName = null)
    {
        if (fileName == null)
        {
            if (string.IsNullOrEmpty(Program.BuildToolOptions.DotNetPath))
            {
                fileName = "dotnet";
            }
            else
            {
                fileName = Program.BuildToolOptions.DotNetPath;
            }
        }
        
        StartInfo.FileName = fileName;
        StartInfo.CreateNoWindow = true;
        StartInfo.ErrorDialog = false;
        StartInfo.UseShellExecute = false;
        StartInfo.RedirectStandardError = true;
        StartInfo.RedirectStandardInput = true;
        StartInfo.RedirectStandardOutput = true;
        EnableRaisingEvents = true;
    }
    
    public bool StartBuildToolProcess()
    {
        StringBuilder output = new StringBuilder();
        OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
            }
        };
            
        ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
            }
        };
            
        if (!Start())
        {
            throw new Exception("Failed to start process");
        }
            
        BeginErrorReadLine();
        BeginOutputReadLine();
        WaitForExit();

        if (ExitCode != 0)
        {
            string errorMessage = output.ToString();
            if (string.IsNullOrEmpty(errorMessage))
            {
                errorMessage = "BuildTool process exited with non-zero exit code, but no output was captured.";
            }
            
            throw new Exception($"BuildTool process failed with exit code {ExitCode}:\n{errorMessage}");
        }
        
        Close();
        return true;
    }
}
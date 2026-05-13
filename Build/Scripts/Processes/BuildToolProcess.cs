using System;
using System.Diagnostics;
using System.Text;

namespace UnrealSharp.Automation.Processes;

public class BuildToolProcess : Process
{
    public BuildToolProcess(string fileName)
    {
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
        StringBuilder Output = new StringBuilder();
        OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                Output.AppendLine(e.Data);
            }
        };
            
        ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                Output.AppendLine(e.Data);
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
            string ErrorMessage = Output.ToString();
            if (string.IsNullOrEmpty(ErrorMessage))
            {
                ErrorMessage = "BuildTool process exited with non-zero exit code, but no output was captured.";
            }
            
            throw new Exception($"BuildTool process failed with exit code {ExitCode}:\n{ErrorMessage}");
        }
        
        Close();
        return true;
    }
}
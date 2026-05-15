using System;
using System.Diagnostics;
using System.Text;
using UnrealSharp.Automation.Utilities;

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
    
    public void StartProcess()
    {
        StringBuilder Output = new StringBuilder();
        OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                LoggerUtilities.LogUnrealSharpInfo(e.Data);
            }
        };
            
        ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                LoggerUtilities.LogUnrealSharpError(e.Data);
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
            throw new Exception($"Process exited with code {ExitCode}");
        }
        
        Close();
    }
}
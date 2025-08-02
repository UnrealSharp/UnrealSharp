using System.Collections;
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
        try
        {
            OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    Console.Out.WriteLine(e.Data);
                }
            };
            
            ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    Console.Error.WriteLine(e.Data);
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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            return false;
        }

        return true;
    }
}
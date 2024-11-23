using System.Diagnostics;
using System.Text;

namespace UnrealSharpBuildTool;

public class BuildToolProcess : Process
{
    public BuildToolProcess(string? fileName = null)
    {
        if (fileName == null)
        {
            fileName = Program.BuildToolOptions.DotNetPath ?? "dotnet";
        }
        
        StartInfo.FileName = fileName;
        StartInfo.RedirectStandardOutput = true;
        StartInfo.RedirectStandardError = true;
        StartInfo.UseShellExecute = false;
        StartInfo.CreateNoWindow = true;
    }

    private void WriteOutProcess()
    {
        string command = StartInfo.FileName;
        string arguments = string.Join(" ", StartInfo.ArgumentList);
        Console.WriteLine($"Command: {command} {arguments}");
    }
    
    public bool StartBuildToolProcess()
    {
        try
        {
            if (!Start())
            {
                throw new Exception("Failed to start process");
            }
            WriteOutProcess();

            https://learn.microsoft.com/de-de/dotnet/api/system.diagnostics.process.standardoutput?view=net-8.0
            StringBuilder output = new();
            this.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    output.AppendLine(args.Data);
                }
            };
            // To avoid deadlocks, use an asynchronous read operation on at least one of the streams.
            BeginOutputReadLine();

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
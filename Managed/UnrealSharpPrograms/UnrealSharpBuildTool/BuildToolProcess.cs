using System.Diagnostics;
using System.Text;

namespace UnrealSharpBuildTool;

public class BuildToolProcess : Process
{

    public string Output { get; private set; } = string.Empty;
    public string Error { get; private set; } = string.Empty;

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

            StringBuilder output = new();
            OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    output.AppendLine(args.Data);
                }
            };

            // To avoid deadlocks, use an asynchronous read operation on at least one of the streams.
            BeginOutputReadLine();

            Error = StandardError.ReadToEnd();

            WaitForExit();
            Output = output.ToString();

            if (ExitCode != 0)
            {
                throw new Exception($"Error in executing build command {StartInfo.Arguments}: {Environment.NewLine + Error + Environment.NewLine + output}");
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

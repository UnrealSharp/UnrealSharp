using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace UnrealSharpScriptGenerator.Utilities;

public static class UnrealSharpBuildTool
{
    public static void Invoke(string action, Dictionary<string, string>? arguments = null)
    {
        string dotNetExe = DotNetUtilities.FindDotNetExecutable();
        string projectName = Path.GetFileNameWithoutExtension(Program.Factory.Session.ProjectFile)!;

        string args = string.Empty;
        args += $"\"{Program.PluginDirectory}/Binaries/Managed/UnrealSharpBuildTool.dll\"";
        args += $" --Action {action}";
        args += $" --EngineDirectory \"{Program.Factory.Session.EngineDirectory}/\"";
        args += $" --ProjectDirectory \"{Program.Factory.Session.ProjectDirectory}/\"";
        args += $" --ProjectName {projectName}";
        args += $" --PluginDirectory \"{Program.PluginDirectory}\"";
        args += $" --DotNetPath \"{dotNetExe}\"";
        
        if (arguments != null)
        {
            args += " --AdditionalArgs";
            foreach (var argument in arguments)
            {
                args += $" {argument.Key}={argument.Value}";
            }
        }
        
        Process process = new Process();
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            FileName = dotNetExe,
            Arguments = args
        };
        
        process.StartInfo = startInfo;
        process.Start();
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

namespace UnrealSharpScriptGenerator.Utilities;

public static class DotNetUtilities
{
    public static string FindDotNetExecutable()
    {
		const string DOTNET_WIN = "dotnet.exe";
		const string DOTNET_UNIX = "dotnet";

		var dotnetExe = OperatingSystem.IsWindows() ? DOTNET_WIN : DOTNET_UNIX;

    	var pathVariable = Environment.GetEnvironmentVariable("PATH");
    
    	if (pathVariable == null)
    	{
    		throw new Exception($"Couldn't find {dotnetExe}!");
    	}
    
    	var paths = pathVariable.Split(Path.PathSeparator);
    
    	foreach (var path in paths)
    	{
    		// This is a hack to avoid using the dotnet.exe from the Unreal Engine installation directory.
    		// Can't use the dotnet.exe from the Unreal Engine installation directory because it's .NET 6.0
    		if (!path.Contains(@"\dotnet\"))
    		{
    			continue;
    		}
    		
    		var dotnetExePath = Path.Combine(path, dotnetExe);
    		
    		if (File.Exists(dotnetExePath))
    		{
    			return dotnetExePath;
    		}
    	}

    	if ( OperatingSystem.IsMacOS() ) {
			if ( File.Exists( "/usr/local/share/dotnet/dotnet" ) ) {
				return "/usr/local/share/dotnet/dotnet";
			}
			if ( File.Exists( "/opt/homebrew/bin/dotnet" ) ) {
				return "/opt/homebrew/bin/dotnet";
			}
		}

		throw new Exception($"Couldn't find {dotnetExe} in PATH!");
    }
    
    public static void BuildSolution(string projectRootDirectory)
    {
    	if (!Directory.Exists(projectRootDirectory))
    	{
    		throw new Exception($"Couldn't find project root directory: {projectRootDirectory}");
    	}
	    
    	Collection<string> arguments = new Collection<string>
	    {
		    "publish",
		    $"\"{projectRootDirectory}\"",
		    $"-p:PublishDir=\"{Program.ManagedBinariesPath}\""
	    };
	    
	    InvokeDotNet(arguments);
    }

    public static void InvokeDotNet(Collection<string> arguments)
    {
	    string dotnetPath = FindDotNetExecutable();
    	
	    Process process = new Process();
	    process.StartInfo.FileName = dotnetPath;
	    
	    foreach (var argument in arguments)
	    {
		    process.StartInfo.ArgumentList.Add(argument);
	    }
	    
	    process.Start();
	    process.WaitForExit();
    	
	    if (process.ExitCode != 0)
	    {
		    throw new Exception($"Failed to invoke dotnet with arguments: {string.Join(" ", arguments)}");
	    }
    }

    public static void InvokeUSharpBuildTool(string action, Dictionary<string, string>? additionalArguments = null)
    {
	    string dotNetExe = FindDotNetExecutable();
	    string projectName = Path.GetFileNameWithoutExtension(Program.Factory.Session.ProjectFile)!;

	    Collection<string> arguments = new Collection<string>
	    {
		    $"{Program.PluginDirectory}/Binaries/Managed/UnrealSharpBuildTool.dll",
		    
		    "--Action",
		    action,
		    
		    "--EngineDirectory",
		    $"{Program.Factory.Session.EngineDirectory}",
		    
		    "--ProjectDirectory",
		    $"{Program.Factory.Session.ProjectDirectory}",
		    
		    "--ProjectName",
		    projectName,
		    
		    "--PluginDirectory",
		    $"{Program.PluginDirectory}",
		    
		    "--DotNetPath",
		    $"{dotNetExe}"
	    };
	    
	    if (additionalArguments != null)
	    {
		    arguments.Add("--AdditionalArgs");
		    
		    foreach (var argument in additionalArguments)
		    {
			    arguments.Add($"{argument.Key}={argument.Value}");
		    }
	    }
	    
	    InvokeDotNet(arguments);
    }
}
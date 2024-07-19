using System;
using System.Diagnostics;
using System.IO;

namespace UnrealSharpScriptGenerator.Utilities;

public static class DotNetUtilities
{
    public static string FindDotNetExecutable()
    {
    	var pathVariable = Environment.GetEnvironmentVariable("PATH");
    
    	if (pathVariable == null)
    	{
    		throw new Exception("Couldn't find dotnet.exe!");
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
    		
    		var dotnetExePath = Path.Combine(path, "dotnet.exe");
    		
    		if (File.Exists(dotnetExePath))
    		{
    			return dotnetExePath;
    		}
    	}

    	throw new Exception("Couldn't find dotnet.exe!");
    }
    
    public static void BuildSolution(string projectRootDirectory)
    {
    	if (!Directory.Exists(projectRootDirectory))
    	{
    		throw new Exception($"Couldn't find project root directory: {projectRootDirectory}");
    	}
    	
    	string dotnetPath = FindDotNetExecutable();
    	
    	Process process = new Process();
    	process.StartInfo.FileName = dotnetPath;
    	
    	process.StartInfo.ArgumentList.Add("publish");
    	process.StartInfo.ArgumentList.Add($"\"{projectRootDirectory}\"");
    	
    	process.StartInfo.ArgumentList.Add("-warn:1");
    	process.StartInfo.ArgumentList.Add($"-p:PublishDir=\"{Program.ManagedBinariesPath}\"");
    	
	    Console.WriteLine("Compiling generated C# code...");
    	process.Start();
    	process.WaitForExit();
    	
    	if (process.ExitCode != 0)
    	{
    		throw new Exception($"Failed to publish solution: {projectRootDirectory}");
    	}
    }
}
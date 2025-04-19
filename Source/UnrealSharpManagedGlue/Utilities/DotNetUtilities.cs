using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace UnrealSharpScriptGenerator.Utilities;

public static class DotNetUtilities
{
	const string DOTNET_MAJOR_VERSION = "9";
	
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

    private static string GetLatestDotNetSdkPath()
    {
        string dotNetExecutable = FindDotNetExecutable();
        string dotNetExecutableDirectory = Path.GetDirectoryName(dotNetExecutable)!;
        string dotNetSdkDirectory = Path.Combine(dotNetExecutableDirectory, "sdk");

        string[] folderPaths = Directory.GetDirectories(dotNetSdkDirectory);

        Version? highestVersion = null;
        foreach (string folderPath in folderPaths)
        {
            string folderName = Path.GetFileName(folderPath);

            if (string.IsNullOrEmpty(folderName) || !char.IsDigit(folderName[0]))
            {
                continue;
            }

            if (Version.TryParse(folderName, out Version? version) && version.Major.ToString() == DOTNET_MAJOR_VERSION)
            {
                if (highestVersion == null || version > highestVersion)
                {
                    highestVersion = version;
                }
            }
        }

        if (highestVersion == null)
        {
            throw new Exception($"Failed to find the latest .NET SDK version for major version {DOTNET_MAJOR_VERSION}.");
        }

        return Path.Combine(dotNetSdkDirectory, highestVersion.ToString());
    }

    public static void BuildSolution(string projectRootDirectory)
    {
    	if (!Directory.Exists(projectRootDirectory))
    	{
    		throw new Exception($"Couldn't find project root directory: {projectRootDirectory}");
    	}
	    
	    if (!Directory.Exists(Program.ManagedBinariesPath))
	    {
		    Directory.CreateDirectory(Program.ManagedBinariesPath);
	    }
	    
    	Collection<string> arguments = new Collection<string>
		{
			"publish",
			$"-p:PublishDir=\"{Program.ManagedBinariesPath}\""
		};

	    InvokeDotNet(arguments, projectRootDirectory);
    }
    
    public static void InvokeDotNet(Collection<string> arguments, string? workingDirectory = null)
    {
	    string dotnetPath = FindDotNetExecutable();
	    var startInfo = new ProcessStartInfo
	    {
		    FileName = dotnetPath,
		    RedirectStandardOutput = true,
		    RedirectStandardError = true,
		    UseShellExecute = false,
		    CreateNoWindow = true
	    };

	    foreach (string argument in arguments)
	    {
		    startInfo.ArgumentList.Add(argument);
	    }
	    
	    if (workingDirectory != null)
	    {
		    startInfo.WorkingDirectory = workingDirectory;
	    }

	    // Set the MSBuild environment variables to the latest .NET SDK that U# supports.
	    {
		    string latestDotNetSdkPath = GetLatestDotNetSdkPath();
		    startInfo.Environment["MSBuildExtensionsPath"] = latestDotNetSdkPath;
		    startInfo.Environment["MSBUILD_EXE_PATH"] = $@"{latestDotNetSdkPath}\MSBuild.dll";
		    startInfo.Environment["MSBuildSDKsPath"] = $@"{latestDotNetSdkPath}\Sdks";
	    }
	    
	    using (Process process = new Process())
	    {
		    process.StartInfo = startInfo;

		    try
		    {
			    process.Start();
		    }
		    catch (Exception ex)
		    {
			    throw new Exception($"Failed to start process '{dotnetPath}' with arguments: {string.Join(" ", startInfo.ArgumentList)}", ex);
		    }

		    string standardOutput = process.StandardOutput.ReadToEnd();
		    string standardError = process.StandardError.ReadToEnd();

		    process.WaitForExit();

		    if (process.ExitCode == 0)
		    {
			    return;
		    }

		    string errorDetails = $@"
Failed to invoke dotnet command:
Executable: {dotnetPath}
Arguments: {string.Join(" ", startInfo.ArgumentList)}
Exit Code: {process.ExitCode}
Standard Output: {standardOutput}
Standard Error: {standardError}";

		    throw new Exception(errorDetails);
	    }
    }

    public static void InvokeUSharpBuildTool(string action, Dictionary<string, string>? additionalArguments = null)
    {
	    string dotNetExe = FindDotNetExecutable();
	    string projectName = Path.GetFileNameWithoutExtension(Program.Factory.Session.ProjectFile)!;
	    string unrealSharpBuildToolPath = Path.Combine(Program.ManagedBinariesPath, "UnrealSharpBuildTool.dll");
	    
	    if (!File.Exists(unrealSharpBuildToolPath))
	    {
		    throw new Exception($"Failed to find UnrealSharpBuildTool.dll at: {unrealSharpBuildToolPath}");
	    }

	    Collection<string> arguments = new Collection<string>
	    {
		    unrealSharpBuildToolPath,
		    
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
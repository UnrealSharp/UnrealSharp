using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace UnrealSharp.Shared;

public static class DotNetUtilities
{
	public const string DOTNET_MAJOR_VERSION = "9.0";
	public const string DOTNET_MAJOR_VERSION_DISPLAY = "net" + DOTNET_MAJOR_VERSION;

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

    public static string GetLatestDotNetSdkPath()
    {
	    string dotNetExecutable = FindDotNetExecutable();
	    string dotNetExecutableDirectory = Path.GetDirectoryName(dotNetExecutable)!;
	    string dotNetSdkDirectory = Path.Combine(dotNetExecutableDirectory!, "sdk");

	    string[] folderPaths = Directory.GetDirectories(dotNetSdkDirectory);

	    string? versionName = null;
	    Version latestVersion = new Version(0, 0);
	    
	    foreach (string folderPath in folderPaths)
	    {
		    string folderName = Path.GetFileName(folderPath);

		    if (!Version.TryParse(folderName, out var version))
		    {
			    continue;
		    }
		    
		    string versionString = version.ToString();
		    if (!versionString.StartsWith(DOTNET_MAJOR_VERSION) || version <= latestVersion)
		    {
			    continue;
		    }
		    
		    latestVersion = version;
		    versionName = folderName;
	    }
	    
	    if (versionName == null)
	    {
		    throw new Exception($"Couldn't find .NET SDK version starting with {DOTNET_MAJOR_VERSION} in {dotNetSdkDirectory}");
	    }

	    return Path.Combine(dotNetSdkDirectory, versionName);
    }

    public static void BuildSolution(string projectRootDirectory)
    {
    	if (!Directory.Exists(projectRootDirectory))
    	{
    		throw new Exception($"Couldn't find project root directory: {projectRootDirectory}");
    	}

    	Collection<string> arguments = new Collection<string> { "build" };
	    InvokeDotNet(arguments, projectRootDirectory);
    }

    public static bool InvokeDotNet(Collection<string> arguments, string? workingDirectory = null)
    {
	    string dotnetPath = FindDotNetExecutable();

	    ProcessStartInfo startInfo = new ProcessStartInfo
	    {
		    FileName = dotnetPath,
		    RedirectStandardOutput = true,
		    RedirectStandardError = true
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
	    // Otherwise, we'll use the .NET SDK that comes with the Unreal Engine.
	    {
		    string latestDotNetSdkPath = GetLatestDotNetSdkPath();
		    startInfo.Environment["MSBuildExtensionsPath"] = latestDotNetSdkPath;
		    startInfo.Environment["MSBUILD_EXE_PATH"] = $@"{latestDotNetSdkPath}\MSBuild.dll";
		    startInfo.Environment["MSBuildSDKsPath"] = $@"{latestDotNetSdkPath}\Sdks";
	    }

        // Disable roll forward to avoid using wrong .NET runtimes, this propagates to child processes.
        startInfo.Environment["DOTNET_ROLL_FORWARD"] = "LatestMinor";

	    using Process process = new Process();
	    process.StartInfo = startInfo;

	    try
	    {
		    StringBuilder outputBuilder = new StringBuilder();
		    process.OutputDataReceived += (sender, e) =>
		    {
			    if (e.Data != null)
			    {
				    outputBuilder.AppendLine(e.Data);
			    }
		    };
            
		    process.ErrorDataReceived += (sender, e) =>
		    {
			    if (e.Data != null)
			    {
				    outputBuilder.AppendLine(e.Data);
			    }
		    };
            
		    if (!process.Start())
		    {
			    throw new Exception("Failed to start process");
		    }
            
		    process.BeginErrorReadLine();
		    process.BeginOutputReadLine();
		    process.WaitForExit();

		    if (process.ExitCode != 0)
		    {
			    string errorMessage = outputBuilder.ToString();
			    
			    if (string.IsNullOrEmpty(errorMessage))
			    {
				    errorMessage = "Process exited with non-zero exit code but no output was captured.";
			    }
			    
			    throw new Exception($"Process failed with exit code {process.ExitCode}: {errorMessage}. Arguments: {string.Join(" ", arguments)}" );
		    }
	    }
	    catch (Exception ex)
	    {
		    Console.WriteLine($"An error occurred: {ex.Message}");
		    return false;
	    }
	    
	    return true;
    }

    public static bool InvokeUSharpBuildTool(string action,
	    string managedBinariesPath,
	    string projectName,
	    string pluginDirectory,
	    string projectDirectory,
	    string engineDirectory,
	    IEnumerable<KeyValuePair<string, string>>? additionalArguments = null)
    {
	    string dotNetExe = FindDotNetExecutable();
	    string unrealSharpBuildToolPath = Path.Combine(managedBinariesPath, "UnrealSharpBuildTool.dll");

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
		    $"{engineDirectory}",

		    "--ProjectDirectory",
		    $"{projectDirectory}",

		    "--ProjectName",
		    projectName,

		    "--PluginDirectory",
		    $"{pluginDirectory}",

		    "--DotNetPath",
		    $"{dotNetExe}"
	    };

	    if (additionalArguments != null)
	    {
		    arguments.Add("--AdditionalArgs");

		    foreach (KeyValuePair<string, string> argument in additionalArguments)
		    {
			    arguments.Add($"{argument.Key}={argument.Value}");
		    }
	    }

	    return InvokeDotNet(arguments);
    }
}

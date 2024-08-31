using System;
using System.Diagnostics;
using System.IO;
using UnrealBuildTool;

public class CSharpForUE : ModuleRules
{
	private readonly string _managedPath;
	private readonly string _managedBinariesPath;
	private readonly string _engineGluePath;
	
	public CSharpForUE(ReadOnlyTargetRules Target) : base(Target)
	{
		PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;
		_managedPath = Path.Combine(PluginDirectory, "Managed");
		_managedBinariesPath = Path.Combine(PluginDirectory, "Binaries", "Managed");
		_engineGluePath = Path.Combine(_managedPath, "UnrealSharp", "UnrealSharp", "Generated");
		
		PublicDefinitions.Add("GENERATED_GLUE_PATH=" + _engineGluePath);
		PublicDefinitions.Add("PLUGIN_PATH=" + PluginDirectory);
		PublicDefinitions.Add("BUILDING_EDITOR=" + (Target.bBuildEditor ? "1" : "0"));
		
		PublicDependencyModuleNames.AddRange(
			new string[]
			{
				"Core",
			}
			);
		
		PrivateDependencyModuleNames.AddRange(
			new string[]
			{
				"CoreUObject",
				"Engine",
				"Slate",
				"SlateCore", 
				"Boost", 
				"XmlParser",
				"Json", 
				"Projects",
				"UMG", 
				"DeveloperSettings", 
				"UnrealSharpProcHelper", 
				"EnhancedInput", 
				"UnrealSharpUtilities",
				"GameplayTags"
			}
			);

        PublicIncludePaths.AddRange(new string[] { ModuleDirectory });

        IncludeDotNetHeaders();

		if (Target.bBuildEditor)
		{
			PrivateDependencyModuleNames.AddRange(new string[]
			{
				"UnrealEd", 
				"EditorSubsystem", 
			});
			
			PublishSolution(Path.Combine(_managedPath, "UnrealSharpPrograms"));
		}
		
		if (Target.bGenerateProjectFiles && Directory.Exists(_engineGluePath))
		{
			PublishSolution(Path.Combine(_managedPath, "UnrealSharp"));
		}
	}
	
	private void IncludeDotNetHeaders()
	{
		PublicSystemIncludePaths.Add(Path.Combine(_managedPath, "DotNetRuntime", "inc"));
	}
	
	public static string FindDotNetExecutable()
	{
		const string DOTNET_WIN = "dotnet.exe";
		const string DOTNET_UNIX = "dotnet";

		var dotnetExe = OperatingSystem.IsWindows() ? DOTNET_WIN : DOTNET_UNIX;

		var pathVariable = Environment.GetEnvironmentVariable("PATH");
    
		if (pathVariable == null)
		{
			return null;
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

	void PublishSolution(string projectRootDirectory)
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
		
		process.StartInfo.ArgumentList.Add($"-p:PublishDir=\"{_managedBinariesPath}\"");

		// process.StartInfo.ArgumentList.Add($"-p:Configuration=\"Development\"");
		// process.StartInfo.ArgumentList.Add($"-p:Platform=\"Any CPU\"");
		
		process.Start();
		process.WaitForExit();
		
		if (process.ExitCode != 0)
		{
			// throw new Exception($"Failed to publish solution: {projectRootDirectory}");
			Console.WriteLine($"Failed to publish solution: {projectRootDirectory}");
		}
	}
}



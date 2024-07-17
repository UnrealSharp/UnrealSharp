using System;
using System.Diagnostics;
using System.IO;
using UnrealBuildTool;

public class CSharpForUE : ModuleRules
{
	private string ManagedPath;
	private string ManagedBinariesPath;
	
	public CSharpForUE(ReadOnlyTargetRules Target) : base(Target)
	{
		PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;
		ManagedPath = Path.Combine(PluginDirectory, "Managed");
		ManagedBinariesPath = Path.Combine(PluginDirectory, "Binaries", "Managed");
		
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
			
			string generatedGluePath = Path.Combine(ManagedPath, "UnrealSharp", "UnrealSharp", "Generated");
			PublicDefinitions.Add("GENERATED_GLUE_PATH=" + generatedGluePath);
			PublicDefinitions.Add("PLUGIN_PATH=" + PluginDirectory);
			
			PublishSolution(Path.Combine(ManagedPath, "UnrealSharpPrograms"));
		}
	}
	
	private void IncludeDotNetHeaders()
	{
		PublicSystemIncludePaths.Add(Path.Combine(ManagedPath, "DotNetRuntime", "inc"));
	}
	
	public static string FindDotNetExecutable()
	{
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
			
			var dotnetExePath = Path.Combine(path, "dotnet.exe");
			
			if (File.Exists(dotnetExePath))
			{
				return dotnetExePath;
			}
		}

		throw new Exception("Couldn't find dotnet.exe!");
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
		
		process.StartInfo.ArgumentList.Add($"-p:PublishDir=\"{ManagedBinariesPath}\"");
		
		process.Start();
		process.WaitForExit();
		
		if (process.ExitCode != 0)
		{
			throw new Exception($"Failed to publish solution: {projectRootDirectory}");
		}
	}
	
}



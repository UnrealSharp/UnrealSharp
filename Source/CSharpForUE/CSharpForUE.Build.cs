using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using UnrealBuildTool;

enum BuildConfiguration
{
	Debug,
	Release
}

public class CSharpForUE : ModuleRules
{
	
	private string ManagedPath;
	
	public CSharpForUE(ReadOnlyTargetRules Target) : base(Target)
	{
		PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;
		ManagedPath = Path.Combine(PluginDirectory, "Managed");
		
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
			}
			);

        PublicIncludePaths.AddRange(new string[] { ModuleDirectory });

        IncludeDotNetHeaders();

		if (Target.bBuildEditor)
		{
			PrivateDependencyModuleNames.AddRange(new string[]
			{
				"UnrealEd", 
				"GlueGenerator",
				"EditorSubsystem", 
			});
		}
		
		IncludeDotNetHeaders();

		if (Target.Type == TargetRules.TargetType.Editor)
		{
			BuildPrograms();
		}
		
		if (Target.Type == TargetRules.TargetType.Game)
		{
			string ManagedBinariesPath = Path.Combine(PluginDirectory, "Binaries", "Managed");
			string[] dependencies = Directory.GetFiles(ManagedBinariesPath, "*.*", SearchOption.AllDirectories);
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

	void BuildPrograms()
	{
		string outputPath = Path.Combine(PluginDirectory, "Binaries", "Managed");
		BuildSolution(Path.Combine(ManagedPath, "UnrealSharpPrograms", "UnrealSharpPrograms.sln"), BuildConfiguration.Release, outputPath);
		Console.WriteLine("UnrealSharpPrograms built successfully!");
	}
	
	void StartDotNetProcess(Collection<string> arguments)
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
	}
	
	void BuildSolution(string solutionPath, BuildConfiguration buildConfiguration = BuildConfiguration.Debug, string outputDirectory = null)
	{
		if (!File.Exists(solutionPath))
		{
			throw new Exception($"Couldn't find the solution file at \"{solutionPath}\"");
		}
		
		string dotnetPath = FindDotNetExecutable();
		
		Process process = new Process();
		process.StartInfo.FileName = dotnetPath;
		
		process.StartInfo.ArgumentList.Add("build");
		process.StartInfo.ArgumentList.Add($"\"{solutionPath}\"");
		
		process.StartInfo.ArgumentList.Add("-c");
		process.StartInfo.ArgumentList.Add(buildConfiguration.ToString());
		
		if (outputDirectory != null)
		{
			process.StartInfo.ArgumentList.Add("-o");
			process.StartInfo.ArgumentList.Add(outputDirectory);
		}
		
		process.Start();
		process.WaitForExit();
		
		Console.WriteLine("Successfully built solution at: \"{0}\" ", solutionPath);
	}
	
}



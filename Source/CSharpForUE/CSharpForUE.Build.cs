using System;
using System.Diagnostics;
using System.IO;
using UnrealBuildTool;

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
			}
			);
		
		if (Target.Type == TargetRules.TargetType.Editor)
		{
			PrivateDependencyModuleNames.AddRange(new string[]
			{
				"GlueGenerator",
				"UnrealEd", 
				"EditorSubsystem", 
			});
		}
		
		IncludeDotNetHeaders();
		
		if (Target.Type == TargetRules.TargetType.Editor)
		{
			BuildPrograms();
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
		string dotnetPath = FindDotNetExecutable();
		
		Process process = new Process();
		process.StartInfo.FileName = dotnetPath;
		
		process.StartInfo.ArgumentList.Add("build");
		
		string slnPath = Path.Combine(ManagedPath, "UnrealSharpPrograms", "UnrealSharpPrograms.sln");
		
		if (!File.Exists(slnPath))
		{
			throw new Exception($"Couldn't find the solution file for UnrealSharpPrograms at \"{slnPath}\"");
		}
		
		process.StartInfo.ArgumentList.Add($"\"{slnPath}\"");
		
		process.StartInfo.ArgumentList.Add("-c");
		process.StartInfo.ArgumentList.Add("Release");

		process.Start();
		process.WaitForExit();
		
		Console.WriteLine("UnrealSharpPrograms built successfully!");
	}
	
	void IncludeUnrealSharpBinaries()
	{
		// Get the project's binaries folder
		
	}
}



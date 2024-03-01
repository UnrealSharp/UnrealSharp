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
		
		IncludeDotNetHeaders();

		if (Target.Type == TargetRules.TargetType.Editor)
		{
			PrivateDependencyModuleNames.AddRange(new string[]
			{
				"GlueGenerator",
				"UnrealEd", 
				"EditorSubsystem", 
			});
		}
	}

	private void IncludeDotNetHeaders()
	{
		PublicSystemIncludePaths.Add(Path.Combine(ManagedPath, "DotNetRuntime", "inc"));
	}
}



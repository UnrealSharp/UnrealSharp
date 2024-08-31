using UnrealBuildTool;

public class UnrealSharpEditor : ModuleRules
{
    public UnrealSharpEditor(ReadOnlyTargetRules Target) : base(Target)
    {
        PCHUsage = ModuleRules.PCHUsageMode.UseExplicitOrSharedPCHs;

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
                "EditorSubsystem", 
                "CSharpForUE",
                "UnrealEd", 
                "UnrealSharpProcHelper",
                "BlueprintGraph",
                "ToolMenus",
                "EditorFramework",
                "InputCore",
			}
        );
    }
}
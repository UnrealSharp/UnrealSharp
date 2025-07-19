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
                "UnrealSharpBinds"
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
                "UnrealSharpCore",
                "UnrealEd",
                "UnrealSharpProcHelper",
                "BlueprintGraph",
                "ToolMenus",
                "EditorFramework",
                "InputCore",
                "AppFramework",
                "EditorStyle",
                "Projects",
                "GameplayTags",
                "DeveloperSettings",
                "UnrealSharpBlueprint",
                "Kismet",
                "KismetCompiler",
                "BlueprintEditorLibrary",
                "SubobjectDataInterface",
                "AssetTools",
                "UnrealSharpRuntimeGlue",
            }
        );

        PublicDefinitions.Add("SkipGlueGeneration");
    }
}

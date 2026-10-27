using UnrealBuildTool;

public class UnrealSharpEditor : ModuleRules
{
    public UnrealSharpEditor(ReadOnlyTargetRules Target) : base(Target)
    {
        PCHUsage = ModuleRules.PCHUsageMode.UseExplicitOrSharedPCHs;

        // Match the Automation project's capability check, including custom engine branches.
        string StagingHandler = System.IO.Path.Combine(EngineDirectory, "Source", "Programs", "AutomationTool", "AutomationUtils", "CustomStagingHandler.cs");
        PublicDefinitions.Add("UNREALSHARP_WITH_CUSTOM_STAGING=" + (System.IO.File.Exists(StagingHandler) ? "1" : "0"));

        PublicDependencyModuleNames.AddRange(
            new string[]
            {
                "Core",
                "UnrealSharpBinds", 
                "UnrealSharpCore"
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
                "UnrealEd",
                "UnrealSharpUtilities",
                "BlueprintGraph",
                "ToolMenus",
                "EditorFramework",
                "InputCore",
                "AppFramework",
                "EditorStyle",
                "Projects",
                "GameplayTags",
                "DeveloperSettings",
                "UnrealSharpAsyncBlueprint",
                "Kismet",
                "KismetCompiler",
                "BlueprintEditorLibrary",
                "SubobjectDataInterface",
                "AssetTools",
                "PluginBrowser", 
                "UnrealSharpUtilities", 
                "PlacementMode",
                "DeveloperToolSettings",
                "UMG",
                "ToolWidgets"
            }
        );

        PublicDefinitions.Add("ForceAsEngineGlue=1");
    }
}

using UnrealBuildTool;

public class UnrealSharpEditor : ModuleRules
{
    public UnrealSharpEditor(ReadOnlyTargetRules Target) : base(Target)
    {
        PCHUsage = ModuleRules.PCHUsageMode.UseExplicitOrSharedPCHs;

        bool bSupportsCustomStaging = Target.Version.MajorVersion > 5
            || (Target.Version.MajorVersion == 5 && Target.Version.MinorVersion >= 7);
        PublicDefinitions.Add("UNREALSHARP_WITH_CUSTOM_STAGING=" + (bSupportsCustomStaging ? "1" : "0"));

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

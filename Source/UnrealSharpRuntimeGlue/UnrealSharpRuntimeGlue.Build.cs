using UnrealBuildTool;

public class UnrealSharpRuntimeGlue : ModuleRules
{
    public UnrealSharpRuntimeGlue(ReadOnlyTargetRules Target) : base(Target)
    {
        PCHUsage = ModuleRules.PCHUsageMode.UseExplicitOrSharedPCHs;

        PublicDependencyModuleNames.AddRange(
            new string[]
            {
                "Core", 
                "UnrealSharpEditor",
                "UnrealSharpUtilities"
            }
        );

        PrivateDependencyModuleNames.AddRange(
            new string[]
            {
                "CoreUObject",
                "Engine",
                "Slate",
                "SlateCore",
                "DeveloperSettings",
                "UnrealEd",
                "GameplayTags",
                "UnrealSharpCore"
            }
        );

        PublicDefinitions.Add("SkipGlueGeneration=1");
    }
}

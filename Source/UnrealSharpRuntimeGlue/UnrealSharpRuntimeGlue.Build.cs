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
            }
        );

        PrivateDependencyModuleNames.AddRange(
            new string[]
            {
                "CoreUObject",
                "Engine",
                "Slate",
                "UnrealSharpUtilities",
                "SlateCore",
                "DeveloperSettings",
                "UnrealEd",
                "GameplayTags",
                "UnrealSharpUtilities"
            }
        );

        PublicDefinitions.Add("SkipGlueGeneration=1");
    }
}

using UnrealBuildTool;

public class UnrealSharpAsyncBlueprint : ModuleRules
{
    public UnrealSharpAsyncBlueprint(ReadOnlyTargetRules Target) : base(Target)
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
                "UnrealSharpCore",
                "BlueprintGraph"
            }
        );

        PublicDefinitions.Add("SkipGlueGeneration=1");
    }
}

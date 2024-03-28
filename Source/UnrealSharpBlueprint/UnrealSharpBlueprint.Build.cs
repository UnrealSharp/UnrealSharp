using UnrealBuildTool;

public class UnrealSharpBlueprint : ModuleRules
{
    public UnrealSharpBlueprint(ReadOnlyTargetRules Target) : base(Target)
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
                "CSharpForUE",
                "BlueprintGraph"
            }
        );
    }
}
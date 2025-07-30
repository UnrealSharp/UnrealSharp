using UnrealBuildTool;

public class UnrealSharpCompiler : ModuleRules
{
    public UnrealSharpCompiler(ReadOnlyTargetRules Target) : base(Target)
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
                "UnrealSharpCore",
                "KismetCompiler",
                "Kismet",
                "BlueprintGraph",
                "UnrealEd",
                "DeveloperSettings", 
                "UnrealSharpEditor", 
                "AIModule", 
                "StateTreeModule", 
                "UnrealSharpUtilities"
            }
        );
    }
}
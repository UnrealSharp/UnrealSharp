using UnrealBuildTool;

public class UnrealSharpUtilities : ModuleRules
{
    public UnrealSharpUtilities(ReadOnlyTargetRules Target) : base(Target)
    {
        PCHUsage = ModuleRules.PCHUsageMode.UseExplicitOrSharedPCHs;

        PublicDependencyModuleNames.AddRange(
            new string[]
            {
                "Core", 
                "Json", 
                "Projects",
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
                "Projects"
            }
        );
        
        PublicDefinitions.Add("ForceAsEngineGlue=1");
    }
}
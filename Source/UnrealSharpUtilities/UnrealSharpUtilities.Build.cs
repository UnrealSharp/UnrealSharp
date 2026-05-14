using UnrealBuildTool;

public class UnrealSharpUtilities : ModuleRules
{
    public UnrealSharpUtilities(ReadOnlyTargetRules Target) : base(Target)
    {
        PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;

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
                "Projects", 
            }
        );

        if (Target.bBuildEditor)
        {
            PublicDependencyModuleNames.AddRange(
                new string[]
                {
                    "UATHelper",
                }
            );
        }
        
        PublicDefinitions.Add("ForceAsEngineGlue=1");
    }
}
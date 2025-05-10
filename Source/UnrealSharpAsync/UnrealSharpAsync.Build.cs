using UnrealBuildTool;

public class UnrealSharpAsync : ModuleRules
{
    public UnrealSharpAsync(ReadOnlyTargetRules Target) : base(Target)
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
                "UnrealSharpBinds",
                "UnrealSharpCore"
            }
        );
        
        PublicDefinitions.Add("ForceAsEngineGlue=1");
    }
}
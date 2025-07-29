using UnrealBuildTool;

public class UnrealSharpProcHelper : ModuleRules
{
    public UnrealSharpProcHelper(ReadOnlyTargetRules Target) : base(Target)
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
                "Projects",
                "Json",
                "XmlParser",
            }
        );

        PublicDefinitions.Add("SkipGlueGeneration");
    }
}

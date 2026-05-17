using System.IO;
using AutomationTool;
using UnrealBuildTool;

namespace UnrealSharp.Automation.Utilities;

public static class BuildUtilities
{
    public static string GetIntermediateBuildDirectory(this BuildCommand buildCommand)
    {
        return Path.Combine(buildCommand.GetProjectRootFolder(), "Intermediate", "Build", "UnrealSharp");
    }
    
    public static string GetIntermediateBuildPathForPlatform(BuildCommand buildCommand, UnrealArch architecture, UnrealTargetPlatform configuration, UnrealTargetConfiguration targetConfiguration)
    {
        string ArchitectureString = architecture.GetTargetArchitecture();
        string PlatformString = configuration.GetTargetPlatform();
        string BuildConfigString = targetConfiguration.GetDotNetBuildConfiguration();
        
        string IntermediateBuildDirectory = buildCommand.GetIntermediateBuildDirectory();
        return Path.Combine(IntermediateBuildDirectory, ArchitectureString, PlatformString, BuildConfigString);
    }
}
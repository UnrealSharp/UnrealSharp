using System;
using System.IO;
using AutomationTool;
using UnrealBuildTool;

namespace UnrealSharp.Automation.Utilities;

public static class DotNetSdkUtilities
{
    public static string GetDotNetRuntimeIdentifier(UnrealTargetPlatform platform, UnrealArch architecture)
    {
        string PlatformPart = GetPlatformIdentifier(platform);
        string ArchitecturePart = GetArchitectureIdentifier(architecture);
        return $"{PlatformPart}-{ArchitecturePart}";
    }

    public static string GetPlatformIdentifier(UnrealTargetPlatform platform)
    {
        if (platform == UnrealTargetPlatform.Win64)
        {
            return "win";
        }

        if (platform == UnrealTargetPlatform.Mac)
        {
            return "osx";
        }

        if (platform == UnrealTargetPlatform.Linux)
        {
            return "linux";
        }

        if (platform == UnrealTargetPlatform.LinuxArm64)
        {
            return "linux";
        }

        throw new NotSupportedException($"Unsupported target platform for .NET publish: '{platform}'. " + $"Supported platforms: Win64, Mac, Linux, LinuxArm64.");
    }

    public static string GetArchitectureIdentifier(UnrealArch architecture)
    {
        if (architecture == UnrealArch.X64)
        {
            return "x64";
        }

        if (architecture == UnrealArch.Arm64)
        {
            return "arm64";
        }

        throw new NotSupportedException($"Unsupported target architecture for .NET publish: '{architecture}'. " + $"Supported architectures: X64, Arm64.");
    }
    
    public static string GetTargetArchitecture(this UnrealArch architecture)
    {
        return architecture.ToString();
    }
    
    public static string GetTargetPlatform(this UnrealTargetPlatform platform)
    {
        return platform.ToString();
    }
    
    public static string GetDotNetBuildConfiguration(this UnrealTargetConfiguration configuration)
    {
        if (configuration == UnrealTargetConfiguration.Debug || configuration == UnrealTargetConfiguration.DebugGame)
        {
            return "Debug";
        }

        if (configuration == UnrealTargetConfiguration.Development || configuration == UnrealTargetConfiguration.Test || configuration == UnrealTargetConfiguration.Shipping)
        {
            return "Release";
        }

        throw new ArgumentOutOfRangeException(nameof(configuration), configuration, "Unsupported configuration");
    }
    
    public static void CopyGlobalJson(BuildCommand buildCommand)
    {
        string PluginRootDirectory = buildCommand.GetUnrealSharpRootFolder();
        string ScriptDirectory = buildCommand.GetProjectScriptFolder();
        string SourceGlobalJsonPath = Path.Combine(PluginRootDirectory, "Managed", "global.json");
        string DestinationGlobalJsonPath = Path.Combine(ScriptDirectory, "global.json");
        
        if (!Directory.Exists(ScriptDirectory))
        {
            Directory.CreateDirectory(ScriptDirectory);
        }
        
        File.Copy(SourceGlobalJsonPath, DestinationGlobalJsonPath, overwrite: true);
    }
}
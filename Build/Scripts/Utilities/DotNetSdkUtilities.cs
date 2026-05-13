using System;
using System.IO;
using AutomationTool;
using UnrealBuildTool;

namespace UnrealSharp.Automation.Utilities;

public static class DotNetSdkUtilities
{
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
    
    public static string GetAssemblyExtension()
    {
        if (OperatingSystem.IsWindows())
        {
            return ".dll";
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            return ".so";
        }

        throw new PlatformNotSupportedException("Unsupported platform");
    }
    
    public static void CopyGlobalJson(BuildCommand buildCommand)
    {
        string PluginRootDirectory = buildCommand.GetUnrealSharpRootFolder();
        string ScriptDirectory = buildCommand.GetProjectScriptFolder();
        string SourceGlobalJsonPath = Path.Combine(PluginRootDirectory, "Managed", "global.json");
        string DestinationGlobalJsonPath = Path.Combine(ScriptDirectory, "global.json");
        File.Copy(SourceGlobalJsonPath, DestinationGlobalJsonPath, overwrite: true);
    }
}
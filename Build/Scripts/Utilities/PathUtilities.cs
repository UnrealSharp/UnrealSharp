using System;
using System.Collections.Generic;
using System.IO;
using AutomationTool;
using EpicGames.Core;
using UnrealBuildBase;
using UnrealBuildTool;

namespace UnrealSharp.Automation.Utilities;

public static class PathUtilities
{
    public static void InitPaths(string engineRootDirectory)
    {
        Unreal.LocationOverride.RootDirectory = new DirectoryReference(engineRootDirectory);
    }
    
    public static string GetProjectRootFolder(this BuildCommand command)
    {
        FileReference? Project = command.ParseProjectParam();
        
        if (Project == null)
        {
            throw new Exception("No project file specified. Please specify a project file using the -Project=... parameter.");
        }
        
        return Project.Directory.FullName;
    }
    
    public static string GetUnrealSharpIntermediateDirectory(this BuildCommand buildCommand)
    {
        return Path.Combine(buildCommand.GetProjectRootFolder(), "Intermediate", "UnrealSharp");
    }
    
    public static string GetUhtGeneratedOutputPath(string root, TargetType targetType)
    {
        return Path.Combine(GetIntermediateOutputPath(root), "UHT", targetType.ToString());
    }
    
    public static string GetIntermediateOutputPath(string root)
    {
        return Path.Combine(root, "Intermediate", "UnrealSharp");
    }
    
    public static string GetUnrealSharpRootFolder(this BuildCommand buildCommand)
    {
        FileReference UnrealSharpUPlugin = buildCommand.GetUnrealSharpUPlugin();
        return UnrealSharpUPlugin.Directory.FullName;
    }
    
    public static string BuildOutputPath(string rootDirectory)
    {
        return Path.Combine(rootDirectory, "Binaries", "Managed", DotNetUtilities.GetVersion());
    }
    
    public static string GetUnrealSharpSharedPropsPath(this BuildCommand buildCommand)
    {
        string UnrealSharpRootFolder = GetUnrealSharpRootFolder(buildCommand);
        return Path.Combine(UnrealSharpRootFolder, "UnrealSharp.Shared.props");
    }
    
    public static string QuotePath(string path)
    {
        return path.Contains(' ') ? $"\"{path}\"" : path;
    }
}
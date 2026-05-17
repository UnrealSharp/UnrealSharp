using System;
using System.Collections.Generic;
using System.IO;
using AutomationTool;
using EpicGames.Core;
using UnrealBuildBase;

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
    
    public static FileReference GetUnrealSharpUPlugin(this BuildCommand command)
    {
        FileReference? Project = command.ParseProjectParam();
        IEnumerable<FileReference> FoundPlugins = PluginsBase.EnumeratePlugins(Project);
        
        FileReference? UnrealSharpPlugin = null;
        foreach (FileReference Plugin in FoundPlugins)
        {
            if (Plugin.GetFileName() != "UnrealSharp.uplugin")
            {
                continue;
            }
            
            UnrealSharpPlugin = Plugin;
            break;
        }
        
        if (UnrealSharpPlugin == null)
        {
            throw new Exception("Failed to find UnrealSharp.uplugin in the project plugins folder. Make sure UnrealSharp is properly installed and added to your project.");
        }
        
        return UnrealSharpPlugin;
    }
    
    public static string GetUnrealSharpRootFolder(this BuildCommand buildCommand)
    {
        FileReference UnrealSharpUPlugin = GetUnrealSharpUPlugin(buildCommand);
        return UnrealSharpUPlugin.Directory.FullName;
    }
    
    public static string GetOutputPath(string rootDirectory)
    {
        return Path.Combine(rootDirectory, "Binaries", "Managed", DotNetUtilities.GetVersion());
    }
    
    public static string GetUnrealSharpSharedPropsPath(this BuildCommand buildCommand)
    {
        string UnrealSharpRootFolder = GetUnrealSharpRootFolder(buildCommand);
        return Path.Combine(UnrealSharpRootFolder, "UnrealSharp.Shared.props");
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using AutomationTool;
using EpicGames.Core;
using UnrealBuildBase;

namespace UnrealSharp.Automation.Utilities;

public static class ProjectUtilities
{
    public static string GetProjectName(this BuildCommand buildCommand)
    {
        FileReference Project = buildCommand.GetUProjectFile();
        return Path.GetFileNameWithoutExtension(Project.FullName);
    }
    
    public static string GetScriptFolder(this BuildCommand buildCommand, string rootFolder)
    {
        return Path.Combine(rootFolder, buildCommand.GetScriptDirectoryName());
    }
    
    public static string GetProjectScriptFolder(this BuildCommand buildCommand)
    {
        string ProjectRoot = buildCommand.GetProjectRootFolder();
        return buildCommand.GetScriptFolder(ProjectRoot);
    }
    
    public static bool IsEditorOnlyProject(string csprojPath)
    {
        XmlDocument Doc = new XmlDocument();
        Doc.Load(csprojPath);

        foreach (XmlElement PropertyGroup in Doc.DocumentElement!.SelectNodes("PropertyGroup")!.OfType<XmlElement>())
        {
            XmlElement? Marker = PropertyGroup["IsEditorOnly"];
            if (Marker != null && string.Equals(Marker.InnerText.Trim(), "true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    public static List<FileReference> GetUnrealProjectAndPluginFiles(this BuildCommand buildCommand)
    {
        FileReference Project = buildCommand.GetUProjectFile();
        IEnumerable<FileReference> AllPlugins = PluginsBase.EnumeratePlugins(Project);
        
        List<FileReference> ProjectAndPluginFiles = AllPlugins.ToList();
        ProjectAndPluginFiles.Add(Project!);
        
        return ProjectAndPluginFiles.ToList();
    }
    
    public static List<FileInfo> GetUnrealSharpProjectFiles(this BuildCommand buildCommand, string folder)
    {
        List<FileReference> ProjectAndPluginFiles = GetUnrealProjectAndPluginFiles(buildCommand);
        List<FileInfo> UnrealSharpProjectFiles = new List<FileInfo>();
        
        foreach (FileReference ProjectFile in ProjectAndPluginFiles)
        {
            string ProjectScriptFolder = Path.Combine(ProjectFile.Directory.FullName, folder);

            if (!Directory.Exists(ProjectScriptFolder))
            {
                continue;
            }
            
            IEnumerable<FileInfo> ScriptFiles = GetManagedProjectsInDirectory(new DirectoryInfo(ProjectScriptFolder));
            UnrealSharpProjectFiles.AddRange(ScriptFiles);
        }
        
        return UnrealSharpProjectFiles;
    }
    
    public static IEnumerable<FileReference> GetGameModules(this BuildCommand buildCommand)
    {
        FileReference Project = buildCommand.GetUProjectFile();
        
        string PluginsFolder = Path.Combine(Project.Directory.FullName, "Plugins");
        IEnumerable<FileReference> FoundPlugins = PluginsBase.EnumeratePlugins(new DirectoryReference(PluginsFolder));
        
        List<FileReference> GameModules = new List<FileReference>();
        GameModules.AddRange(FoundPlugins);
        GameModules.Add(Project);
        
        return GameModules;
    }
    
    public static List<FileInfo> GetManagedProjectFiles(this BuildCommand buildCommand)
    {
        return GetUnrealSharpProjectFiles(buildCommand, buildCommand.GetScriptDirectoryName());
    }
    
    private static IEnumerable<FileInfo> GetManagedProjectsInDirectory(DirectoryInfo folder)
    {
        IEnumerable<FileInfo> CsprojFiles = folder.EnumerateFiles("*.csproj", SearchOption.AllDirectories);
        IEnumerable<FileInfo> FsprojFiles = folder.EnumerateFiles("*.fsproj", SearchOption.AllDirectories);
        return CsprojFiles.Concat(FsprojFiles);
    }
    
    public static FileReference GetUnrealSharpUPlugin(this BuildCommand command)
    {
        FileReference Project = command.GetUProjectFile();
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

    public static FileReference GetUProjectFile(this BuildCommand command)
    {
        string ProjectPath = command.ParseRequiredStringParam("Project");
        
        if (!File.Exists(ProjectPath))
        {
            throw new FileNotFoundException($"UProject file not found at path: {ProjectPath}");
        }
        
        return new FileReference(ProjectPath);
    }
    
    public static bool ContainsUPluginOrUProjectFile(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return false;
        }

        foreach (string FilePath in Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly))
        {
            string Extension = Path.GetExtension(FilePath);
            
            if (Extension.Equals(".uplugin", StringComparison.OrdinalIgnoreCase) || Extension.Equals(".uproject", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
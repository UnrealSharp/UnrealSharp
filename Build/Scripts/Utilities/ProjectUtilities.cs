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
        FileReference? Project = buildCommand.ParseProjectParam();
        
        if (Project == null)
        {
            throw new Exception("No project file specified. Please specify a project file using the -Project=... parameter.");
        }
        
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
        FileReference? Project = buildCommand.ParseProjectParam();
        IEnumerable<FileReference> AllPlugins = PluginsBase.EnumeratePlugins(Project);
        
        List<FileReference> ProjectAndPluginFiles = AllPlugins.ToList();
        ProjectAndPluginFiles.Add(Project!);
        
        return ProjectAndPluginFiles.ToList();
    }
    
    public static List<FileInfo> GetUnrealSharpProjectFiles(this BuildCommand buildCommand)
    {
        List<FileReference> ProjectAndPluginFiles = GetUnrealProjectAndPluginFiles(buildCommand);
        List<FileInfo> UnrealSharpProjectFiles = new List<FileInfo>();
        
        foreach (FileReference ProjectFile in ProjectAndPluginFiles)
        {
            string ProjectScriptFolder = buildCommand.GetScriptFolder(ProjectFile.Directory.FullName);

            if (!Directory.Exists(ProjectScriptFolder))
            {
                continue;
            }
            
            IEnumerable<FileInfo> ScriptFiles = GetManagedProjectsInDirectory(new DirectoryInfo(ProjectScriptFolder));
            UnrealSharpProjectFiles.AddRange(ScriptFiles);
        }
        
        return UnrealSharpProjectFiles;
    }
    
    private static IEnumerable<FileInfo> GetManagedProjectsInDirectory(DirectoryInfo folder)
    {
        IEnumerable<FileInfo> CsprojFiles = folder.EnumerateFiles("*.csproj", SearchOption.AllDirectories);
        IEnumerable<FileInfo> FsprojFiles = folder.EnumerateFiles("*.fsproj", SearchOption.AllDirectories);
        return CsprojFiles.Concat(FsprojFiles);
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
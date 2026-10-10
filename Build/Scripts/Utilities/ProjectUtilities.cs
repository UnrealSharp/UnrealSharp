using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using AutomationTool;
using EpicGames.Core;
using UnrealBuildBase;
using UnrealBuildTool;

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
        return GetUnrealProjectAndPluginFiles(buildCommand, ManagedBuildTarget.FromCommand(buildCommand));
    }

    internal static List<FileReference> GetUnrealProjectAndPluginFiles(this BuildCommand buildCommand, ManagedBuildTarget target)
    {
        FileReference Project = buildCommand.GetUProjectFile();
        List<FileReference> Files = GetEnabledPluginFiles(Project, target).ToList();
        Files.Add(Project);
        return Files;
    }

    public static List<FileInfo> GetUnrealSharpProjectFiles(this BuildCommand buildCommand, string folder)
    {
        return GetUnrealSharpProjectFiles(buildCommand, folder, ManagedBuildTarget.FromCommand(buildCommand));
    }

    internal static List<FileInfo> GetUnrealSharpProjectFiles(this BuildCommand buildCommand, string folder, ManagedBuildTarget target)
    {
        List<FileReference> ProjectAndPluginFiles = GetUnrealProjectAndPluginFiles(buildCommand, target);
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

    internal static List<FileInfo> GetManagedProjectFiles(this BuildCommand buildCommand, ManagedBuildTarget target)
    {
        return GetUnrealSharpProjectFiles(buildCommand, buildCommand.GetScriptDirectoryName(), target);
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
    private static IEnumerable<FileReference> GetEnabledPluginFiles(FileReference project, ManagedBuildTarget target)
    {
        ProjectDescriptor Descriptor = ProjectDescriptor.FromFile(project);
        Dictionary<string, PluginInfo> AvailablePlugins = ReadAvailablePlugins(project, Descriptor);
        HashSet<string> EnabledPlugins = new(StringComparer.OrdinalIgnoreCase);
        Queue<PluginInfo> PendingPlugins = new(AvailablePlugins.Values.Where(plugin =>
            Plugins.IsPluginEnabledForTarget(plugin, Descriptor, target.Platform, target.Configuration, target.Type)));

        while (PendingPlugins.TryDequeue(out PluginInfo? Plugin))
        {
            if (!EnabledPlugins.Add(Plugin.Name) || Plugin.Descriptor.Plugins == null)
            {
                continue;
            }

            foreach (PluginReferenceDescriptor Reference in Plugin.Descriptor.Plugins)
            {
                if (!IsDependencyEnabled(Reference, Descriptor, target))
                {
                    continue;
                }

                if (!AvailablePlugins.TryGetValue(Reference.Name, out PluginInfo? Dependency))
                {
                    if (!Reference.bOptional)
                    {
                        throw new AutomationException($"Required plugin '{Reference.Name}' referenced by '{Plugin.Name}' was not found.");
                    }
                    continue;
                }

                if (Dependency.Descriptor.SupportsTargetPlatform(target.Platform))
                {
                    PendingPlugins.Enqueue(Dependency);
                }
            }
        }

        return AvailablePlugins.Values.Where(plugin => EnabledPlugins.Contains(plugin.Name)).Select(plugin => plugin.File);
    }

    private static Dictionary<string, PluginInfo> ReadAvailablePlugins(FileReference project, ProjectDescriptor descriptor)
    {
        return Plugins.ToFilteredDictionary(
                Plugins.ReadAvailablePlugins(Unreal.EngineDirectory, project.Directory, descriptor.AdditionalPluginDirectories))
            .ToDictionary(pair => pair.Key, pair => pair.Value.ChoiceVersion!, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsDependencyEnabled(PluginReferenceDescriptor reference, ProjectDescriptor descriptor, ManagedBuildTarget target)
    {
        PluginReferenceDescriptor? ProjectReference = descriptor.Plugins?.FirstOrDefault(projectReference =>
            string.Equals(projectReference.Name, reference.Name, StringComparison.OrdinalIgnoreCase));
        return target.IsReferenceEnabled(reference)
            && (ProjectReference == null || target.IsReferenceEnabled(ProjectReference));
    }
}

internal sealed record ManagedBuildTarget(
    UnrealTargetPlatform Platform,
    TargetType Type,
    UnrealTargetConfiguration Configuration)
{
    public static ManagedBuildTarget FromCommand(BuildCommand command)
    {
        string Platform = command.ParseParamValue("TargetPlatform", BuildHostPlatform.Current.Platform.ToString());
        string Type = command.ParseParamValue("TargetType", command.ParseParamValue("UETargetType", "Editor"));
        string Configuration = command.ParseParamValue("TargetConfiguration", command.ParseParamValue("UEBuildConfig", "Development"));
        return new ManagedBuildTarget(UnrealTargetPlatform.Parse(Platform),
            Enum.Parse<TargetType>(Type, true), Enum.Parse<UnrealTargetConfiguration>(Configuration, true));
    }

    public bool IsReferenceEnabled(PluginReferenceDescriptor reference)
    {
        return reference.IsEnabledForPlatform(Platform)
            && reference.IsEnabledForTargetConfiguration(Configuration)
            && reference.IsEnabledForTarget(Type);
    }
}

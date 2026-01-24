using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.PropertyTranslators;
using UnrealSharpManagedGlue.SourceGeneration;

namespace UnrealSharpManagedGlue.Utilities;

public struct ProjectDirInfo
{
    private readonly string _projectName;
    private readonly string _projectDirectory;
    public HashSet<string>? Dependencies { get; set; }

    public ProjectDirInfo(string projectName, string projectDirectory, HashSet<string>? dependencies = null)
    {
        _projectName = projectName;
        _projectDirectory = projectDirectory;
        Dependencies = dependencies;
    }
    
    public string GlueProjectName => $"{_projectName}.Glue";
    public string GlueProjectFile => $"{GlueProjectName}.csproj";
    
    public string ScriptDirectory => Path.Combine(_projectDirectory, "Script");
    
    public string GlueCsProjPath => Path.Combine(GlueProjectDirectory, GlueProjectFile);

    public bool IsUProject => _projectDirectory.EndsWith(".uproject", StringComparison.OrdinalIgnoreCase);
    
    public bool IsPartOfEngine => _projectName == "Engine";
    
    public string GlueProjectDirectory => Path.Combine(ScriptDirectory, GlueProjectName);
    
    public string ProjectRoot => _projectDirectory;
}

public static class FileExporter
{
    private static readonly ReaderWriterLockSlim ReadWriteLock = new();
    private static readonly HashSet<string> AffectedFiles = new();

    public static void SaveGlueToDisk(UhtType type, GeneratorStringBuilder stringBuilder)
    {
        string directory = GetDirectoryPath(type.Package);
        SaveGlueToDisk(type.Package, directory, type.EngineName, stringBuilder.ToString());
    }

    public static string GetFilePath(string typeName, string directory)
    {
        return Path.Combine(directory, $"{typeName}.generated.cs");
    }

    public static void SaveGlueToDisk(UhtPackage package, string directory, string typeName, string text)
    {
        string absoluteFilePath = GetFilePath(typeName, directory);
        
        bool needsWrite = true;
        if (File.Exists(absoluteFilePath))
        {
            FileInfo fileInfo = new FileInfo(absoluteFilePath);
            if (fileInfo.Length == text.Length && File.ReadAllText(absoluteFilePath) == text)
            {
                needsWrite = false;
            }
        }
        
        ReadWriteLock.EnterWriteLock();
        try
        {
            AffectedFiles.Add(absoluteFilePath);

            if (!needsWrite)
            {
                return;
            }
            
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(absoluteFilePath, text);

            if (package.IsPartOfEngine())
            {
                CSharpExporter.HasModifiedEngineGlue = true;
            }
        }
        finally
        {
            ReadWriteLock.ExitWriteLock();
        }
    }

    public static void AddUnchangedType(UhtType type)
    {
        string engineName = type.EngineName;
        
        if (type is UhtFunction function)
        {
            engineName = DelegateBasePropertyTranslator.GetDelegateName(function);
        }
        
        string directory = GetDirectoryPath(type.Package);
        string filePath = GetFilePath(engineName, directory);
        AffectedFiles.Add(filePath);

        if (type is UhtStruct uhtStruct && uhtStruct.Functions.Any(f => f.HasMetadata("ExtensionMethod")))
        {
            AffectedFiles.Add(GetFilePath($"{engineName}_Extensions", directory));
        }
    }

    public static string GetDirectoryPath(UhtPackage package)
    {
        if (package == null)
        {
            throw new InvalidOperationException("Package is null");
        }

        string rootPath = GetGluePath(package);
        return Path.Combine(rootPath, package.GetShortName());
    }

    public static string GetGluePath(UhtPackage package)
    {
        ProjectDirInfo projectDirInfo = package.FindOrAddProjectInfo();
        return projectDirInfo.GlueProjectDirectory;
    }

    public static void CleanOldExportedFiles()
    {
        Console.WriteLine("Cleaning up old generated C# glue files...");
        
        CleanFilesInDirectories(GeneratorStatics.EngineGluePath);
        
        foreach (ProjectDirInfo pluginDirectory in PluginUtilities.PluginInfo.Values)
        {
            if (pluginDirectory.IsPartOfEngine)
            {
                continue;
            }
            
            CleanFilesInDirectories(pluginDirectory.GlueProjectDirectory, true);
        }
    }

    public static void CleanModuleFolders()
    {
        CleanGeneratedFolder(GeneratorStatics.EngineGluePath);
        
        foreach (ProjectDirInfo pluginDirectory in Program.PluginDirs)
        {
            CleanGeneratedFolder(pluginDirectory.GlueProjectDirectory);
        }
    }
    
    public static void CleanGeneratedFolder(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        HashSet<string> ignoredDirectories = GetIgnoredDirectories(path);

        // TODO: Move runtime glue to a separate csproj. So we can fully clean the ProjectGlue folder.
        // Below is a temporary solution to not delete runtime glue that can cause compilation errors on editor startup,
        // and avoid having to restore nuget packages.
        string[] directories = Directory.GetDirectories(path);
        foreach (string directory in directories)
        {
            if (IsIntermediateDirectory(directory) || ignoredDirectories.Contains(Path.GetRelativePath(path, directory)))
            {
                continue;
            }

            Directory.Delete(directory, true);
        }
        
        string csprojFile = Path.Combine(path, $"{Path.GetFileName(path)}.csproj");
        if (File.Exists(csprojFile))
        {
            File.Delete(csprojFile);
        }
    }
    
    private static HashSet<string> GetIgnoredDirectories(string path)
    {
        string glueIgnoreFileName = Path.Combine(path, ".glueignore");
        if (!File.Exists(glueIgnoreFileName))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        HashSet<string> ignoredDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using StreamReader fileInput = File.OpenText(glueIgnoreFileName);
        while (!fileInput.EndOfStream)
        {
            string? line = fileInput.ReadLine();
            
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            ignoredDirectories.Add(line.Trim());
        }
        return ignoredDirectories;
    }

    private static void CleanFilesInDirectories(string path, bool recursive = false)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        string[] directories = Directory.GetDirectories(path);
        HashSet<string> ignoredDirectories = GetIgnoredDirectories(path);

        foreach (string directory in directories)
        {
            if (ignoredDirectories.Contains(Path.GetRelativePath(path, directory)))
            {
                continue;
            }

            string moduleName = Path.GetFileName(directory);
            if (!PackageHeadersTracker.HasModuleBeenExported(moduleName))
            {
                continue;
            }

            int removedFiles = 0;
            string[] files = Directory.GetFiles(directory);

            foreach (string file in files)
            {
                if (AffectedFiles.Contains(file))
                {
                    continue;
                }

                File.Delete(file);
                removedFiles++;
            }

            if (removedFiles == files.Length)
            {
                Directory.Delete(directory, recursive);
            }
        }
    }
    static bool IsIntermediateDirectory(string path)
    {
        string directoryName = Path.GetFileName(path);
        return directoryName is "obj" or "bin" or "Properties";
    }
}

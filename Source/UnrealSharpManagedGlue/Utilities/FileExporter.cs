using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.Utilities;

public readonly struct ProjectDirInfo
{
    private readonly string _projectName;
    private readonly string _projectDirectory;
    public HashSet<string>? Dependencies { get; }

    public ProjectDirInfo(string projectName, string projectDirectory, HashSet<string>? dependencies = null)
    {
        _projectName = projectName;
        _projectDirectory = projectDirectory;
        Dependencies = dependencies;
    }
    
    public string GlueProjectName => $"{_projectName}.Glue";
    public string GlueProjectName_LEGACY => $"{_projectName}.PluginGlue";
    public string GlueProjectFile => $"{GlueProjectName}.csproj";
    
    public string ScriptDirectory => Path.Combine(_projectDirectory, "Script");
    
    public string GlueCsProjPath => Path.Combine(GlueProjectDirectory, GlueProjectFile);

    public bool IsUProject => _projectDirectory.EndsWith(".uproject", StringComparison.OrdinalIgnoreCase);
    
    public bool IsPartOfEngine => _projectName == "Engine";
    
    public string GlueProjectDirectory => Path.Combine(ScriptDirectory, GlueProjectName);
    public string GlueProjectDirectory_LEGACY => Path.Combine(ScriptDirectory, GlueProjectName_LEGACY);
    
    public string ProjectRoot => _projectDirectory;
}

public static class FileExporter
{
    private static readonly ReaderWriterLockSlim ReadWriteLock = new();

    private static readonly List<string> ChangedFiles = new();
    private static readonly List<string> UnchangedFiles = new();

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
        bool directoryExists = Directory.Exists(directory);
        bool glueExists = File.Exists(absoluteFilePath);

        ReadWriteLock.EnterWriteLock();
        try
        {
            bool matchingGlue = glueExists && File.ReadAllText(absoluteFilePath) == text;

            // If the directory exists and the file exists with the same text, we can return early
            if (directoryExists && matchingGlue)
            {
                UnchangedFiles.Add(absoluteFilePath);
                return;
            }

            if (!directoryExists)
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(absoluteFilePath, text);
            ChangedFiles.Add(absoluteFilePath);

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
        string directory = GetDirectoryPath(type.Package);
        string filePath = GetFilePath(type.EngineName, directory);
        UnchangedFiles.Add(filePath);

        if (type is UhtStruct uhtStruct && uhtStruct.Functions.Any(f => f.HasMetadata("ExtensionMethod")))
        {
            UnchangedFiles.Add(GetFilePath($"{type.EngineName}_Extensions", directory));
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
        CleanFilesInDirectories(Program.EngineGluePath);
        
        foreach (ProjectDirInfo pluginDirectory in Program.PluginDirs)
        {
            CleanFilesInDirectories(pluginDirectory.GlueProjectDirectory, true);
            CleanFilesInDirectories(pluginDirectory.GlueProjectDirectory_LEGACY, true);
        }
    }

    public static void CleanModuleFolders()
    {
        CleanGeneratedFolder(Program.EngineGluePath);
        
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
            if (string.IsNullOrWhiteSpace(line)) continue;

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

        foreach (var directory in directories)
        {
            if (ignoredDirectories.Contains(Path.GetRelativePath(path, directory)))
            {
                continue;
            }

            string moduleName = Path.GetFileName(directory);
            if (!CSharpExporter.HasBeenExported(moduleName))
            {
                continue;
            }

            int removedFiles = 0;
            string[] files = Directory.GetFiles(directory);

            foreach (var file in files)
            {
                if (ChangedFiles.Contains(file) || UnchangedFiles.Contains(file))
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

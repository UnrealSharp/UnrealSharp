using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.Utilities;

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
    }
    
    public static string GetDirectoryPath(UhtPackage package)
    {
        if (package == null)
        {
            throw new Exception("Package is null");
        }

        string rootPath = package.IsPartOfEngine() ? Program.EngineGluePath : Program.ProjectGluePath;
        return Path.Combine(rootPath, package.GetShortName());
    }
    
    public static void CleanOldExportedFiles()
    {
        Console.WriteLine("Cleaning up old generated C# glue files...");
        CleanFilesInDirectories(Program.EngineGluePath);
        CleanFilesInDirectories(Program.ProjectGluePath, true);
    }
    
    public static void CleanModuleFolders()
    {
        CleanGeneratedFolder(Program.EngineGluePath);
        CleanGeneratedFolder(Program.ProjectGluePath);
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
        string glueignoreFileName = Path.Combine(path, ".glueignore");
        if (!File.Exists(glueignoreFileName)) return [];
        
        HashSet<string> ignoredDirectories = [];
        using StreamReader fileInput = File.OpenText(glueignoreFileName);
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
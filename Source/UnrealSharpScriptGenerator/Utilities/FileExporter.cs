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
            
            if (package.IsPackagePartOfEngine())
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

        string rootPath = package.IsPackagePartOfEngine() ? Program.EngineGluePath : Program.ProjectGluePath;
        return Path.Combine(rootPath, package.GetShortName());
    }
    
    public static void CleanOldExportedFiles()
    {
        Console.WriteLine("Cleaning up old generated C# glue files...");
        CleanFilesInDirectories(Program.EngineGluePath);
        CleanFilesInDirectories(Program.ProjectGluePath, true);
    }
    
    private static void CleanFilesInDirectories(string path, bool recursive = false)
    {
        if (!Directory.Exists(path))
        {
            return;
        }
        
        string[] directories = Directory.GetDirectories(path);
        
        foreach (var directory in directories)
        {
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
}
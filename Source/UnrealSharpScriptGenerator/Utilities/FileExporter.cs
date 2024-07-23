using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.Utilities;

public static class FileExporter
{
    private static readonly ReaderWriterLockSlim ReadWriteLock = new();
    private static readonly List<string> ExportedFiles = new();
    public static bool HasModifiedEngineGlue { get; private set; }
    
    public static void SaveGlueToDisk(UhtType type, GeneratorStringBuilder stringBuilder, string fileName = "")
    {
        UhtPackage package = type.Package;
        string directory = GetDirectoryPath(package);
        string text = stringBuilder.ToString();
        fileName = string.IsNullOrEmpty(fileName) ? type.EngineName : fileName;
        SaveGlueToDisk(directory, fileName, text, package);
    }
    
    public static void SaveGlueToDisk(string directory, string typeName, string text, UhtPackage package)
    {
        string absoluteFilePath = Path.Combine(directory, $"{typeName}.generated.cs");
        
        bool directoryExists = Directory.Exists(directory);
        bool glueExists = File.Exists(absoluteFilePath);
        
        ReadWriteLock.EnterWriteLock();
        try
        {
            bool matchingGlue = glueExists && File.ReadAllText(absoluteFilePath) == text;
            // If the directory exists and the file exists with the same text, we can return early
            if (directoryExists && matchingGlue)
            {
                return;
            }
            
            if (!directoryExists)
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(absoluteFilePath, text);
            
            if (package.IsPartOfEngine)
            {
                lock (typeof(Program))
                {
                    HasModifiedEngineGlue = true;
                }
            }
        }
        finally
        {
            ExportedFiles.Add(absoluteFilePath);
            ReadWriteLock.ExitWriteLock();
        }
    }
    
    public static string GetDirectoryPath(UhtPackage package)
    {
        if (package == null)
        {
            throw new Exception("Package is null");
        }

        string rootPath = package.IsPartOfEngine ? Program.EngineGluePath : Program.ProjectGluePath;
        return Path.Combine(rootPath, package.ShortName);
    }
    
    public static void CleanOldExportedFiles()
    {
        CleanFilesInDirectories(Program.EngineGluePath);
        CleanFilesInDirectories(Program.ProjectGluePath);
    }
    
    private static void CleanFilesInDirectories(string path)
    {
        string[] directories = Directory.GetDirectories(path);
        
        foreach (var directory in directories)
        {
            string[] files = Directory.GetFiles(directory);
            
            foreach (var file in files)
            {
                if (ExportedFiles.Contains(file))
                {
                    continue;
                }
                
                File.Delete(file);
            }
        }
    }
}
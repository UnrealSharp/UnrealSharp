using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.PropertyTranslators;
using UnrealSharpManagedGlue.SourceGeneration;

namespace UnrealSharpManagedGlue.Utilities;

public static class FileExporter
{
    private static readonly ReaderWriterLockSlim ReadWriteLock = new();
    private static readonly HashSet<string> AffectedFiles = new(StringComparer.OrdinalIgnoreCase);

    public static void SaveGlueToDisk(UhtType type, GeneratorStringBuilder stringBuilder)
    {
        string directory =type.Package.GetModuleUhtOutputDirectory();
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
            FileInfo fileInfo = new(absoluteFilePath);
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
        string engineName = type is UhtFunction function ? DelegateBasePropertyTranslator.GetDelegateName(function) : type.EngineName;

        string directory = type.Package.GetModuleUhtOutputDirectory();
        AffectedFiles.Add(GetFilePath(engineName, directory));

        if (type is UhtStruct uhtStruct && uhtStruct.Functions.Any(f => f.HasMetadata("ExtensionMethod")))
        {
            AffectedFiles.Add(GetFilePath($"{engineName}_Extensions", directory));
        }
    }

    public static void CleanOldExportedFiles()
    {
        Console.WriteLine("Cleaning up old generated C# glue files...");

        foreach (ModuleInfo plugin in ModuleUtilities.PackageToModuleInfo.Values)
        {
            CleanOldFilesInDirectories(plugin.GlueModuleDirectory);
        }
    }

    public static void CleanGeneratedFolder(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }
    
        DirectoryInfo root = new DirectoryInfo(path);
        foreach (FileSystemInfo item in root.GetFileSystemInfos())
        {
            if (item is DirectoryInfo dir)
            {
                dir.Delete(true);
            }
            else
            {
                item.Delete();
            }
        }
    }

    private static void CleanOldFilesInDirectories(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        if (!PackageHeadersTracker.HasModuleBeenExported(Path.GetFileName(path)))
        {
            return;
        }
        
        foreach (string directory in Directory.GetDirectories(path))
        {
            CleanOldFilesInDirectories(directory);
        }
        
        string[] files = Directory.GetFiles(path);
        foreach (string file in files)
        {
            if (!AffectedFiles.Contains(file))
            {
                File.Delete(file);
            }
        }
        
        string[] remainingFiles = Directory.GetFiles(path);
        if (remainingFiles.Length == 0)
        {
            Directory.Delete(path, false); 
        }
    }
}
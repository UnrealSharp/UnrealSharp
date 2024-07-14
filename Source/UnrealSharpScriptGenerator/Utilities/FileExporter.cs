using System.Collections.Generic;
using System.IO;
using System.Threading;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.Utilities;

public static class FileExporter
{
    private static readonly ReaderWriterLockSlim ReadWriteLock = new();
    private static readonly List<string> ExportedFiles = new();
    
    public static void SaveGlueToDisk(UhtType type, GeneratorStringBuilder generatorStringBuilder)
    {
        string moduleName = ScriptGeneratorUtilities.GetModuleName(type);
        string typeName = type.EngineName;
        SaveGlueToDisk(moduleName, typeName, generatorStringBuilder.ToString());
    }
    
    public static void SaveGlueToDisk(string moduleName, string typeName, string text)
    {
        string directory = Path.Combine(Program.GeneratedGluePath, moduleName);
        string absoluteFilePath = Path.Combine(directory, $"{typeName}.generated.cs");

        ReadWriteLock.EnterWriteLock();
        try
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            if (File.Exists(absoluteFilePath))
            {
                string existingText = File.ReadAllText(absoluteFilePath);
                if (existingText == text)
                {
                    return;
                }
            }
            
            File.WriteAllText(absoluteFilePath, text);
        }
        finally
        {
            ExportedFiles.Add(absoluteFilePath);
            ReadWriteLock.ExitWriteLock();
        }
    }
    
    public static void CleanOldExportedFiles()
    {
        string generatedPath = Program.GeneratedGluePath;
        string[] directories = Directory.GetDirectories(generatedPath);
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
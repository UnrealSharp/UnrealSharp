using System.IO;
using System.Threading;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.Utilities;

public static class FileExporter
{
    private static readonly ReaderWriterLockSlim ReadWriteLock = new();
    
    public static void SaveTypeToDisk(UhtType type, GeneratorStringBuilder generatorStringBuilder)
    {
        string moduleName = ScriptGeneratorUtilities.GetModuleName(type);
        string typeName = type.EngineName;
        SaveTypeToDisk(moduleName, typeName, generatorStringBuilder.ToString());
    }
    
    public static void SaveTypeToDisk(string moduleName, string typeName, string text)
    {
        string directory = Path.Combine(Program.GeneratedGluePath, moduleName);

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string absoluteFilePath = Path.Combine(directory, $"{typeName}.generated.cs");

        ReadWriteLock.EnterWriteLock();
        try
        {
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
            ReadWriteLock.ExitWriteLock();
        }
    }
}
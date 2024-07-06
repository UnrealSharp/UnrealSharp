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
        string directory = Path.Combine(Program.GeneratedGluePath, moduleName);

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string absoluteFilePath = Path.Combine(directory, $"{type.EngineName}.cs");
        string newText = generatorStringBuilder.ToString();

        ReadWriteLock.EnterWriteLock();
        try
        {
            if (File.Exists(absoluteFilePath))
            {
                string existingText = File.ReadAllText(absoluteFilePath);
                if (existingText == newText)
                {
                    return;
                }
            }
            
            File.WriteAllText(absoluteFilePath, newText);
        }
        finally
        {
            ReadWriteLock.ExitWriteLock();
        }
    }
}
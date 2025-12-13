using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator;

public static class ModuleHeadersTracker
{
    const string ModuleDataFileName = "UnrealSharpModuleData.json";
    
    public class ModuleHeaders
    {
        public Dictionary<string, DateTime> HeaderToWriteTime { get; set; } = new();
        public bool HasBeenExported;
    }
    
    public static bool HasDataFromDisk { get; private set; }
    
    private static Dictionary<string, ModuleHeaders?> _moduleHeadersWriteRecord = new();
    
    public static void DeserializeModuleHeaders()
    {
        if (_moduleHeadersWriteRecord.Count > 0)
        {
            return;
        }
        
        if (!Directory.Exists(Program.EngineGluePath))
        {
            return;
        }

        string outputPath = Path.Combine(Program.PluginModule.OutputDirectory, ModuleDataFileName);

        if (!File.Exists(outputPath))
        {
            return;
        }

        using FileStream fileStream = new FileStream(outputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        Dictionary<string, ModuleHeaders>? jsonValue = JsonSerializer.Deserialize<Dictionary<string, ModuleHeaders>>(fileStream);
        fileStream.Close();

        if (jsonValue == null)
        {
            return;
        }
        
        _moduleHeadersWriteRecord = new Dictionary<string, ModuleHeaders?>(jsonValue!);
        HasDataFromDisk = true;
    }
    
    public static void SerializeModuleData()
    {
        string outputPath = Path.Combine(Program.PluginModule.OutputDirectory, ModuleDataFileName);
        using FileStream filestream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        JsonSerializer.Serialize(filestream, _moduleHeadersWriteRecord);
        filestream.Close();
    }
    
    public static bool HasModuleBeenExported(string moduleName)
    {
        if (_moduleHeadersWriteRecord.TryGetValue(moduleName, out ModuleHeaders? headers))
        {
            return headers != null && headers.HasBeenExported;
        }

        return false;
    }
    
    public static bool HasHeaderChanged(string moduleName, UhtHeaderFile headerPath)
    {
        DateTime currentWriteTime = File.GetLastWriteTimeUtc(headerPath.FilePath);
        
        if (!_moduleHeadersWriteRecord.TryGetValue(moduleName, out ModuleHeaders? headers))
        {
            return true;
        }
        
        if (headers != null && headers.HeaderToWriteTime.TryGetValue(headerPath.FilePath, out DateTime recordedWriteTime))
        {
            return recordedWriteTime != currentWriteTime;
        }

        return true;
    }
    
    public static void RecordHeadersWriteTime(string moduleName, IEnumerable<UhtHeaderFile> headerPaths)
    {
        foreach (UhtHeaderFile headerPath in headerPaths)
        {
            RecordHeaderWriteTime(moduleName, headerPath);
        }
    }
    
    public static void RecordHeaderWriteTime(string moduleName, UhtHeaderFile headerPath)
    {
        DateTime currentWriteTime = File.GetLastWriteTimeUtc(headerPath.FilePath);
        
        if (!_moduleHeadersWriteRecord.TryGetValue(moduleName, out ModuleHeaders? headers) || headers == null)
        {
            headers = new ModuleHeaders();
            _moduleHeadersWriteRecord[moduleName] = headers;
        }
        
        headers.HeaderToWriteTime[headerPath.FilePath] = currentWriteTime;
        headers.HasBeenExported = true;
    }
}
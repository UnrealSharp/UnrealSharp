using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace UnrealSharp.Shared;

public static class UnrealSharpSettingsUtilities
{
    private static Dictionary<string, JsonElement>? _config;
    
    public static void InitializeConfigFile(string projectRoot, string unrealSharpRoot)
    {
        if (_config != null)
        {
            return;
        }
        
        string pluginConfigPath = GetConfigFile(unrealSharpRoot);
        string projectConfigPath = GetConfigFile(projectRoot);

        _config = LoadJsonAsDictionary(pluginConfigPath);
        Dictionary<string, JsonElement> projectDict = File.Exists(projectConfigPath) ? LoadJsonAsDictionary(projectConfigPath) : new Dictionary<string, JsonElement>();
        
        foreach (KeyValuePair<string, JsonElement> kvp in projectDict)
        {
            _config[kvp.Key] = kvp.Value;
        }
    }

    public static JsonElement GetElement(string elementName)
    {
        if (_config == null)
        {
            throw new Exception("Run InitializeConfigFile first.");
        }
        
        return _config[elementName];
    }
    
    static string GetConfigFile(string rootDirectory)
    {
        string configDirectory = Path.Combine(rootDirectory, "Config");
        
        EnumerationOptions enumerationOptions = new EnumerationOptions
        {
            RecurseSubdirectories = true
        };

        string[] foundConfigs = Directory.GetFiles(configDirectory, "UnrealSharp.Settings.json", enumerationOptions);

        if (foundConfigs.Length > 1)
        {
            throw new Exception("Found multiple config files");
        }

        if (foundConfigs.Length == 0)
        {
            return string.Empty;
        }

        return foundConfigs[0];
    }

    static Dictionary<string, JsonElement> LoadJsonAsDictionary(string path)
    {
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? new Dictionary<string, JsonElement>();
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AutomationTool;

namespace UnrealSharp.Automation.Utilities;

public static class UnrealSharpSettingsUtilities
{
    private static Dictionary<string, JsonElement>? _config;
    
    static void InitializeConfigFile(BuildCommand buildCommand)
    {
        if (_config != null)
        {
            return;
        }
        
        string PluginConfigPath = GetConfigFile(buildCommand.GetUnrealSharpRootFolder());
        string ProjectConfigPath = GetConfigFile(buildCommand.GetProjectRootFolder());

        _config = LoadJsonAsDictionary(PluginConfigPath);
        Dictionary<string, JsonElement> ProjectDict = File.Exists(ProjectConfigPath) ? LoadJsonAsDictionary(ProjectConfigPath) : new Dictionary<string, JsonElement>();
        
        foreach (KeyValuePair<string, JsonElement> Kvp in ProjectDict)
        {
            _config[Kvp.Key] = Kvp.Value;
        }
    }

    public static JsonElement GetElement(this BuildCommand buildCommand, string elementName)
    {
        InitializeConfigFile(buildCommand);
        
        if (_config == null)
        {
            throw new Exception("Run InitializeConfigFile first.");
        }
        
        return _config[elementName];
    }
    
    static string GetConfigFile(string rootDirectory)
    {
        string ConfigDirectory = Path.Combine(rootDirectory, "Config");
        
        EnumerationOptions EnumerationOptions = new EnumerationOptions
        {
            RecurseSubdirectories = true
        };

        string[] FoundConfigs = Directory.GetFiles(ConfigDirectory, "UnrealSharp.Settings.json", EnumerationOptions);

        if (FoundConfigs.Length > 1)
        {
            throw new Exception("Found multiple config files");
        }

        if (FoundConfigs.Length == 0)
        {
            return string.Empty;
        }

        return FoundConfigs[0];
    }

    static Dictionary<string, JsonElement> LoadJsonAsDictionary(string path)
    {
        string Json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(Json) ?? new Dictionary<string, JsonElement>();
    }
}

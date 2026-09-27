using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace UnrealSharpManagedGlue.Utilities;

public static class UnrealSharpSettingsUtilities
{
    public const string SkipGlueModulesKey = "SkipGlueModules";

    private static Dictionary<string, JsonElement>? _config;
    private static HashSet<string>? _skipGlueModules;
    
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
        if (!TryGetElement(elementName, out JsonElement element))
        {
            throw new KeyNotFoundException($"No UnrealSharp setting named '{elementName}' was found.");
        }

        return element;
    }

    public static bool TryGetElement(string elementName, out JsonElement element)
    {
        if (_config == null)
        {
            throw new Exception("Run InitializeConfigFile first.");
        }

        return _config.TryGetValue(elementName, out element);
    }

    public static bool ShouldSkipGlueGeneration(string moduleName)
    {
        return GetSkipGlueModules().Contains(moduleName);
    }

    public static IReadOnlySet<string> GetSkipGlueModules()
    {
        if (_skipGlueModules != null)
        {
            return _skipGlueModules;
        }

        HashSet<string> modules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (TryGetElement(SkipGlueModulesKey, out JsonElement element) && element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement moduleElement in element.EnumerateArray())
            {
                if (moduleElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                string? moduleName = moduleElement.GetString();
                if (!string.IsNullOrWhiteSpace(moduleName))
                {
                    modules.Add(moduleName);
                }
            }
        }

        _skipGlueModules = modules;
        return modules;
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

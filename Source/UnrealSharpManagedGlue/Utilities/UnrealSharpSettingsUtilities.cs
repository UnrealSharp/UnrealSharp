using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace UnrealSharpManagedGlue.Utilities;

public static class UnrealSharpSettingsUtilities
{
    private static Dictionary<string, JsonElement>? _config;

    public static IReadOnlyList<string> ExcludedHeaderPaths { get; private set; } = Array.Empty<string>();
    public static List<string> ConfigPaths { get; } = new();

    public static void Reset()
    {
        _config = null;
        ExcludedHeaderPaths = Array.Empty<string>();
        ConfigPaths.Clear();
    }
    
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

        ConfigPaths.Add(pluginConfigPath);
        if (File.Exists(projectConfigPath)) ConfigPaths.Add(projectConfigPath);

        List<string> exclusions = new();
        if (_config.TryGetValue("ExcludedHeaderPaths", out JsonElement paths))
        {
            foreach (JsonElement path in paths.EnumerateArray())
            {
                string value = path.GetString()?.Trim() ?? string.Empty;
                if (value.Length == 0)
                    throw new FormatException("ExcludedHeaderPaths must contain non-empty paths.");
                exclusions.Add("/" + value.Replace('\\', '/').TrimStart('/'));
            }
        }
        ExcludedHeaderPaths = exclusions;
    }

    // Settings paths are complete path suffixes, independent of the engine installation root.
    // Project UnrealSharp.Settings.json can replace the list, including with [] to disable it.
    public static bool IsHeaderExcluded(string headerPath)
    {
        string normalized = "/" + headerPath.Replace('\\', '/').TrimStart('/');
        foreach (string path in ExcludedHeaderPaths)
        {
            // A trailing slash selects a directory; file paths match complete suffixes.
            if (path.EndsWith("/", StringComparison.Ordinal)
                ? normalized.Contains(path, StringComparison.OrdinalIgnoreCase)
                : normalized.EndsWith(path, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
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
        if (!Directory.Exists(configDirectory))
        {
            return string.Empty;
        }
        
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

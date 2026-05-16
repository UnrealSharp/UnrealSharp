using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutomationTool;
using UnrealBuildBase;

namespace UnrealSharp.Automation.Utilities;

public static class LaunchSettingsUtilities
{
    public static void CreateOrUpdateLaunchSettings(BuildCommand buildCommand, string launchSettingsPath)
    {
        Root Root = new Root();

        string ExecutablePath = string.Empty;
        if (OperatingSystem.IsWindows())
        {
            ExecutablePath = Path.Combine(Unreal.EngineDirectory.FullName, "Binaries", "Win64", "UnrealEditor.exe");
        }
        else if (OperatingSystem.IsMacOS())
        {
            ExecutablePath = Path.Combine(Unreal.EngineDirectory.FullName, "Binaries", "Mac", "UnrealEditor");
        }

        string? ProjectParam = buildCommand.ParseProjectParam()?.FullName;
        string CommandLineArgs = ProjectParam != null ? $"\"{ProjectParam}\"" : string.Empty;

        Root.Profiles.ProfileName = new Profile
        {
            CommandName = "Executable",
            ExecutablePath = ExecutablePath,
            CommandLineArgs = CommandLineArgs
        };
        
        JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping 
        };

        string JsonString = JsonSerializer.Serialize(Root, Options);
        File.WriteAllText(launchSettingsPath, JsonString);
    }
}

public class Root
{
    [JsonPropertyName("profiles")]
    public Profiles Profiles { get; set; } = new Profiles();
}
public class Profiles
{
    [JsonPropertyName("UnrealSharp")]
    public Profile ProfileName { get; set; } = new Profile();
}

public class Profile
{
    [JsonPropertyName("commandName")]
    public string CommandName { get; set; } = string.Empty;

    [JsonPropertyName("executablePath")]
    public string ExecutablePath { get; set; } = string.Empty;

    [JsonPropertyName("commandLineArgs")]
    public string CommandLineArgs { get; set; } = string.Empty;
}
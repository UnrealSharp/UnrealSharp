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
    private const string ExecutableVerb = "Executable";
    
    public static void CreateOrUpdateLaunchSettings(BuildCommand buildCommand, string launchSettingsPath)
    {
        Root Root = new Root();

        string DevelopmentExecutablePath = string.Empty;
        string DebugExecutablePath = string.Empty;

        if (OperatingSystem.IsWindows())
        {
            string Win64Dir = Path.Combine(Unreal.EngineDirectory.FullName, "Binaries", "Win64");
            DevelopmentExecutablePath = Path.Combine(Win64Dir, "UnrealEditor.exe");
            DebugExecutablePath = Path.Combine(Win64Dir, "UnrealEditor-Win64-DebugGame.exe");
        }
        else if (OperatingSystem.IsMacOS())
        {
            string MacDir = Path.Combine(Unreal.EngineDirectory.FullName, "Binaries", "Mac");
            DevelopmentExecutablePath = Path.Combine(MacDir, "UnrealEditor");
            DebugExecutablePath = Path.Combine(MacDir, "UnrealEditor-Mac-DebugGame");
        }

        string ProjectParam = buildCommand.GetUProjectFile().FullName;
        string CommandLineArgs = $"\"{ProjectParam}\"";

        Root.Profiles.Development = new Profile
        {
            CommandName = ExecutableVerb,
            ExecutablePath = DevelopmentExecutablePath,
            CommandLineArgs = CommandLineArgs
        };

        Root.Profiles.Debug = new Profile
        {
            CommandName = ExecutableVerb,
            ExecutablePath = DebugExecutablePath,
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
    [JsonPropertyName("Development")]
    public Profile Development { get; set; } = new Profile();

    [JsonPropertyName("Debug")]
    public Profile Debug { get; set; } = new Profile();
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
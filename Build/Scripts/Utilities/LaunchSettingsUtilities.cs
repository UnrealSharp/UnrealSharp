using System;
using System.IO;
using AutomationTool;
using Newtonsoft.Json;
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

        string CommandLineArgs = buildCommand.ParseProjectParam()!.FullName;
        
        if (Root.Profiles == null)
        {
            Root.Profiles = new Profiles();
        }

        Root.Profiles.ProfileName = new Profile
        {
            CommandName = "Executable",
            ExecutablePath = ExecutablePath,
            CommandLineArgs = $"\"{CommandLineArgs}\"",
        };

        string NewJsonString = JsonConvert.SerializeObject(Root, Formatting.Indented);
        StreamWriter Writer = File.CreateText(launchSettingsPath);
        Writer.Write(NewJsonString);
        Writer.Close();
    }
}

public class Root
{
    [JsonProperty("profiles")]
    public Profiles Profiles { get; set; } = new Profiles();
}
public class Profiles
{
    [JsonProperty("UnrealSharp")]
    public Profile ProfileName { get; set; } = new Profile();
}

public class Profile
{
    [JsonProperty("commandName")]
    public string CommandName { get; set; } = string.Empty;

    [JsonProperty("executablePath")]
    public string ExecutablePath { get; set; } = string.Empty;

    [JsonProperty("commandLineArgs")]
    public string CommandLineArgs { get; set; } = string.Empty;
}
using System.Text.Json;
using AutomationTool;

namespace UnrealSharp.Automation.Utilities;

public static class CommonUnrealSharpSettingsUtilities
{
    public static string GetScriptDirectoryName(this BuildCommand buildCommand)
    {
        JsonElement ScriptDirectoryName = buildCommand.GetElement("ScriptDirectoryName");
        return ScriptDirectoryName.GetString()!;
    }
}
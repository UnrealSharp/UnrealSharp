using System.Text.Json;

namespace UnrealSharpManagedGlue.Utilities;

public static class CommonUnrealSharpSettings
{
    public static string ScriptDirectoryName 
    {
        get
        {
            JsonElement scriptDirectoryName = SessionStatic.Settings.GetElement("ScriptDirectoryName");
            return scriptDirectoryName.GetString()!;
        }
    }
}
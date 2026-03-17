using System.Text.Json;

namespace UnrealSharp.Shared;

public class CommonUnrealSharpSettings
{
    public static string ScriptDirectoryName 
    {
        get
        {
            JsonElement scriptDirectoryName = UnrealSharpSettingsUtilities.GetElement("ScriptDirectoryName");
            return scriptDirectoryName.GetString()!;
        }
    }
}
using System.Text.Json;
using UnrealSharp.Shared;

namespace UnrealSharpManagedGlue.Utilities;

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
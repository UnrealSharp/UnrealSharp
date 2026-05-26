using System.IO;
using AutomationTool;

namespace UnrealSharp.Automation.Utilities;

public static class LaunchSettingsScaffolding
{
    private const string PropertiesFolderName = "Properties";
    private const string LaunchSettingsFileName = "launchSettings.json";
    
    public static void EnsureProjectLaunchSettings(BuildCommand command, string projectFolderName)
    {
        string CsProjectPath = Path.Combine(command.GetProjectScriptFolder(), projectFolderName);
        string PropertiesDirectoryPath = Path.Combine(CsProjectPath, PropertiesFolderName);
        string LaunchSettingsPath = Path.Combine(PropertiesDirectoryPath, LaunchSettingsFileName);

        if (!Directory.Exists(PropertiesDirectoryPath))
        {
            Directory.CreateDirectory(PropertiesDirectoryPath);
        }

        if (File.Exists(LaunchSettingsPath))
        {
            return;
        }

        LaunchSettingsUtilities.CreateOrUpdateLaunchSettings(command, LaunchSettingsPath);
    }
}
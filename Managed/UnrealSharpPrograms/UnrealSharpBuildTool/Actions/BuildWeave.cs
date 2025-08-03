namespace UnrealSharpBuildTool.Actions;

public class BuildWeave : BuildToolAction
{
    public override bool RunAction()
    {
        BuildSolution buildSolution = new BuildUserSolution();
        WeaveProject weaveProject = new WeaveProject();
        return buildSolution.RunAction() && weaveProject.RunAction() && AddLaunchSettings();
    }
    bool AddLaunchSettings()
    {
        List<FileInfo> allProjectFiles = Program.GetAllProjectFiles(new DirectoryInfo(Program.GetProjectDirectory()));

        foreach (FileInfo projectFile in allProjectFiles)
        {
            if (projectFile.Directory!.Name == "ProjectGlue")
            {
                continue;
            }
            string csProjectPath = Path.Combine(Program.GetScriptFolder(), projectFile.Directory.Name);
            string propertiesDirectoryPath = Path.Combine(csProjectPath, "Properties");
            string launchSettingsPath = Path.Combine(propertiesDirectoryPath, "launchSettings.json");
            if (!Directory.Exists(propertiesDirectoryPath))
            {
                Directory.CreateDirectory(propertiesDirectoryPath);
            }
            if (File.Exists(launchSettingsPath))
            {
                return true;
            }
            Program.CreateOrUpdateLaunchSettings(launchSettingsPath);
        }
        return true;
    }

}

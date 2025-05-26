namespace UnrealSharpBuildTool.Actions;

public static class BuildWeaveAction
{
    [Action("BuildWeave", "Builds the solution and weaves the projects")]
    public static void BuildWeave()
    {
        BuildSolutionAction.BuildSolutionParameters buildSolutionAction = new BuildSolutionAction.BuildSolutionParameters();
        buildSolutionAction.Folders = [Program.GetScriptFolder()];
        BuildSolutionAction.BuildSolution(buildSolutionAction);
        
        Weaving.WeaveParameters weaveParameters = new Weaving.WeaveParameters();
        Weaving.WeaveProject(weaveParameters);

        AddLaunchSettings();
    } 
    
    static void AddLaunchSettings()
    {
        List<FileInfo> allProjectFiles = Program.GetAllProjectFiles(new DirectoryInfo(Program.GetScriptFolder()));

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
                return;
            }
            
            Program.CreateOrUpdateLaunchSettings(launchSettingsPath);
        }
    }
    
}
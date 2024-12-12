using Newtonsoft.Json;

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
        DirectoryInfo scriptFolder = new DirectoryInfo(Program.GetScriptFolder());
        FileInfo[] csprojFiles = scriptFolder.GetFiles("*.csproj", SearchOption.AllDirectories);
        FileInfo[] fsprojFiles = scriptFolder.GetFiles("*.fsproj", SearchOption.AllDirectories);
        List<FileInfo> allProjectFiles = new List<FileInfo>(csprojFiles.Length + fsprojFiles.Length);
        allProjectFiles.AddRange(csprojFiles);
        allProjectFiles.AddRange(fsprojFiles);
        foreach (FileInfo projectFile in allProjectFiles)
        {
            if (projectFile.Directory.Name == "ProjectGlue")
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
        
            CreateOrUpdateLaunchSettings(launchSettingsPath);
        }
        return true;
    }
    
    void CreateOrUpdateLaunchSettings(string launchSettingsPath)
    {
        Root root = new Root();

        string executablePath = string.Empty;
        if (OperatingSystem.IsWindows())
        {
            executablePath = Path.Combine(Program.BuildToolOptions.EngineDirectory, "Binaries", "Win64", "UnrealEditor.exe");
        }
        else if (OperatingSystem.IsMacOS())
        {
            executablePath = Path.Combine(Program.BuildToolOptions.EngineDirectory, "Binaries", "Mac", "UnrealEditor");
        }
        string commandLineArgs = Program.FixPath(Program.GetUProjectFilePath());
        
        // Create a new profile if it doesn't exist
        if (root.Profiles == null)
        {
            root.Profiles = new Profiles();
        }
            
        root.Profiles.ProfileName = new Profile
        {
            CommandName = "Executable",
            ExecutablePath = executablePath,
            CommandLineArgs = $"\"{commandLineArgs}\"",
        };
        
        string newJsonString = JsonConvert.SerializeObject(root, Newtonsoft.Json.Formatting.Indented);
        StreamWriter writer = File.CreateText(launchSettingsPath);
        writer.Write(newJsonString);
        writer.Close();
    }
}
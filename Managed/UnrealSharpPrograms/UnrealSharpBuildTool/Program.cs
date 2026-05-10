using System.Diagnostics;
using Newtonsoft.Json;
using UnrealSharp.Shared;
using UnrealSharpBuildTool.Actions;
using UnrealSharpBuildTool.Exceptions;

namespace UnrealSharpBuildTool;

public static class Program
{
    public static int Main(string[] args)
    {
        Console.WriteLine("UnrealSharpBuildTool Initializing...");
        Stopwatch stopwatch = Stopwatch.StartNew();
    
        try
        {
            BuildToolOptions.ParseArguments(args);
            UnrealSharpSettingsUtilities.InitializeConfigFile(BuildToolOptionsInstance.ProjectDirectory,BuildToolOptionsInstance.PluginDirectory);
        
            ActionManager.RunAction(BuildToolOptionsInstance.Action, BuildToolOptionsInstance.ActionArgs.ToArray());
        
            stopwatch.Stop();
            Console.WriteLine($"\nAction '{BuildToolOptionsInstance.Action}' completed in {stopwatch.Elapsed.TotalSeconds:F2}s.");
            return 0;
        }
        catch (BuildToolException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[Build Error] {ex.Message}");
            Console.ResetColor();
            return 1;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"\n[Exception] {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Console.ResetColor();
            return -1;
        }
    }
    
    private static BuildToolOptions BuildToolOptionsInstance => BuildToolOptions.Instance;

    public static string GetUProjectFilePath()
    {
        return Path.Combine(BuildToolOptionsInstance.ProjectDirectory, BuildToolOptionsInstance.ProjectName + ".uproject");
    }
    
    public static string GetScriptFolder()
    {
        return Path.Combine(BuildToolOptionsInstance.ProjectDirectory, CommonUnrealSharpSettings.ScriptDirectoryName);
    }

    public static string GetPluginsFolder()
    {
        return Path.Combine(BuildToolOptionsInstance.ProjectDirectory, "Plugins");
    }
    
    public static string GetPluginDirectory()
    {
        return BuildToolOptionsInstance.PluginDirectory;
    }

    public static string GetProjectDirectory()
    {
        return BuildToolOptionsInstance.ProjectDirectory;
    }

    public static string FixPath(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return path.Replace('/', '\\');
        }

        return path;
    }

    public static string GetProjectNameAsManaged()
    {
        return "Managed" + BuildToolOptionsInstance.ProjectName;
    }

    public static string GetOutputPath(string rootDir = "", bool includeVersion = true)
    {
        if (string.IsNullOrEmpty(rootDir))
        {
            rootDir = BuildToolOptionsInstance.ProjectDirectory;
        }

        if (includeVersion)
        {
            return Path.Combine(rootDir, "Binaries", "Managed", GetVersion());
        }
        else
        {
            return Path.Combine(rootDir, "Binaries", "Managed");
        }
    }
    
    public static string IntermediateDirectory => Path.Combine(BuildToolOptionsInstance.PluginDirectory, "Intermediate");
    public static string IntermediateBuildDirectory => Path.Combine(BuildToolOptionsInstance.PluginDirectory, "Intermediate", "Build");
    
    public static string GetIntermediateBuildPathForPlatform(TargetArchitecture architecture, TargetPlatform configuration, TargetConfiguration targetConfiguration)
    {
        string architectureString = architecture.GetTargetArchitecture();
        string platformString = configuration.GetTargetPlatform();
        string buildConfigString = targetConfiguration.GetDotNetBuildConfiguration();
        return Path.Combine(IntermediateBuildDirectory, architectureString, platformString, buildConfigString);
    }
    
    public static string GetUnrealSharpSharedProps()
    {
        return Path.GetFullPath(Path.Combine(BuildToolOptionsInstance.PluginDirectory, "UnrealSharp.Shared.props"));
    }

    public static string GetVersion()
    {
        Version currentVersion = Environment.Version;
        string currentVersionStr = $"{currentVersion.Major}.{currentVersion.Minor}";
        return "net" + currentVersionStr;
    }

    public static void CreateOrUpdateLaunchSettings(string launchSettingsPath)
    {
        Root root = new Root();

        string executablePath = string.Empty;
        if (OperatingSystem.IsWindows())
        {
            executablePath = Path.Combine(BuildToolOptionsInstance.EngineDirectory, "Binaries", "Win64", "UnrealEditor.exe");
        }
        else if (OperatingSystem.IsMacOS())
        {
            executablePath = Path.Combine(BuildToolOptionsInstance.EngineDirectory, "Binaries", "Mac", "UnrealEditor");
        }
        string commandLineArgs = FixPath(GetUProjectFilePath());

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

        string newJsonString = JsonConvert.SerializeObject(root, Formatting.Indented);
        StreamWriter writer = File.CreateText(launchSettingsPath);
        writer.Write(newJsonString);
        writer.Close();
    }

    public static List<FileInfo> GetAllProjectFiles(DirectoryInfo folder)
    {
        string scriptDirectoryName = CommonUnrealSharpSettings.ScriptDirectoryName;
        
        return folder.GetDirectories(scriptDirectoryName)
                .SelectMany(GetProjectsInDirectory)
                .Concat(folder.GetDirectories("Plugins")
                        .SelectMany(x => x.EnumerateFiles("*.uplugin", SearchOption.AllDirectories))
                        .Select(x => x.Directory)
                        .Select(x => x!.GetDirectories(scriptDirectoryName).FirstOrDefault())
                        .Where(x => x is not null)
                        .SelectMany(GetProjectsInDirectory!))
                .ToList();
    }

    public static Dictionary<string, List<FileInfo>> GetProjectFilesByDirectory(DirectoryInfo folder)
    {
        Dictionary<string, List<FileInfo>> result = new Dictionary<string, List<FileInfo>>();
        DirectoryInfo? scriptsFolder = folder.GetDirectories(CommonUnrealSharpSettings.ScriptDirectoryName).FirstOrDefault();
        
        if (scriptsFolder is not null)
        {
            result.Add(GetOutputPathForDirectory(scriptsFolder), GetProjectsInDirectory(scriptsFolder).ToList());
        }

        foreach (DirectoryInfo? pluginFolder in folder.GetDirectories("Plugins")
                         .SelectMany(x => x.EnumerateFiles("*.uplugin", SearchOption.AllDirectories))
                         .Select(x => x.Directory)
                         .Select(x => x!.GetDirectories(CommonUnrealSharpSettings.ScriptDirectoryName).FirstOrDefault())
                         .Where(x => x is not null))
        {
            result.Add(GetOutputPathForDirectory(pluginFolder!), GetProjectsInDirectory(pluginFolder!).ToList());
        }

        return result;
    }

    private static string GetOutputPathForDirectory(DirectoryInfo directory)
    {
        return Path.Combine(directory.Parent!.FullName, "Binaries", "Managed");
    }

    private static IEnumerable<FileInfo> GetProjectsInDirectory(DirectoryInfo folder)
    {
        IEnumerable<FileInfo> csprojFiles = folder.EnumerateFiles("*.csproj", SearchOption.AllDirectories);
        IEnumerable<FileInfo> fsprojFiles = folder.EnumerateFiles("*.fsproj", SearchOption.AllDirectories);
        return csprojFiles.Concat(fsprojFiles);
    }
    
    public static void CopyGlobalJson()
    {
        string sourceGlobalJsonPath = Path.Combine(BuildToolOptionsInstance.PluginDirectory, "Managed", "global.json");
        string destinationGlobalJsonPath = Path.Combine(GetScriptFolder(), "global.json");
        File.Copy(sourceGlobalJsonPath, destinationGlobalJsonPath, overwrite: true);
    }
}

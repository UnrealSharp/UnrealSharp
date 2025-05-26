using System.Diagnostics;
using CommandLine;
using Newtonsoft.Json;
using UnrealSharpBuildTool.Actions;

namespace UnrealSharpBuildTool;

public static class Program
{
    public static BuildToolOptions BuildToolOptions = null!;

    public static int Main(string[] args)
    {
        try
        {
            Console.WriteLine("Running UnrealSharpBuildTool...");
            
            Parser parser = new Parser(with => with.HelpWriter = null);
            ParserResult<BuildToolOptions> result = parser.ParseArguments<BuildToolOptions>(args);
            
            if (result.Tag == ParserResultType.NotParsed)
            {
                BuildToolOptions.PrintHelp(result);
                
                string errors = string.Empty;
                foreach (Error error in result.Errors)
                {
                    if (error is TokenError tokenError)
                    {
                        errors += $"{tokenError.Tag}: {tokenError.Token} \n";
                    }
                }
                
                throw new Exception($"Invalid arguments. Errors: {errors}");
            }
            
            BuildToolOptions = result.Value;
            
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            ActionManager.RunAction(result.Value.Action, result.Value.ActionArgs);
            stopwatch.Stop();
            
            Console.WriteLine($"UnrealSharpBuildTool executed {BuildToolOptions.Action} action successfully.");
            Console.WriteLine($"Execution time: {stopwatch.Elapsed.TotalSeconds} seconds");
        }
        catch (Exception exception)
        {
            Console.WriteLine("An error occurred: " + exception.Message + Environment.NewLine + exception.StackTrace);
            return 1;
        }
        
        return 0;
    }
    
    public static string GetSolutionFile()
    {
        return Path.Combine(GetScriptFolder(), BuildToolOptions.ProjectName + ".sln");
    }

    public static string GetUProjectFilePath()
    {
        return Path.Combine(BuildToolOptions.ProjectDirectory, BuildToolOptions.ProjectName + ".uproject");
    }
    
    public static string GetScriptFolder()
    {
        return Path.Combine(BuildToolOptions.ProjectDirectory, "Script");
    }
    
    public static string GetProjectDirectory()
    {
        return BuildToolOptions.ProjectDirectory;
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
        return "Managed" + BuildToolOptions.ProjectName;
    }
    
    public static string GetOutputPath(string rootDir = "")
    {
        if (string.IsNullOrEmpty(rootDir))
        {
            rootDir = BuildToolOptions.ProjectDirectory;
        }
        
        return Path.Combine(rootDir, "Binaries", "Managed");
    }

    public static string GetWeaver()
    {
        return Path.Combine(GetManagedBinariesDirectory(), "UnrealSharpWeaver.dll");
    }

    public static string GetManagedBinariesDirectory()
    {
        return Path.Combine(BuildToolOptions.PluginDirectory, "Binaries", "Managed");
    }
    
    public static string IntermediateDirectory => Path.Combine(BuildToolOptions.PluginDirectory, "Intermediate");
    public static string IntermediateBuildDirectory => Path.Combine(BuildToolOptions.PluginDirectory, "Intermediate", "Build");
    
    public static string GetIntermediateBuildPathForPlatform(TargetArchitecture architecture, TargetPlatform configuration, TargetConfiguration targetConfiguration)
    {
        string architectureString = architecture.GetTargetArchitecture();
        string platformString = configuration.GetTargetPlatform();
        string buildConfigString = targetConfiguration.ToString().ToLowerInvariant();
        return Path.Combine(IntermediateBuildDirectory, architectureString, platformString, buildConfigString);
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

    public static List<FileInfo> GetAllProjectFiles(DirectoryInfo folder)
    {
        FileInfo[] csprojFiles = folder.GetFiles("*.csproj", SearchOption.AllDirectories);
        FileInfo[] fsprojFiles = folder.GetFiles("*.fsproj", SearchOption.AllDirectories);
        List<FileInfo> allProjectFiles = new List<FileInfo>(csprojFiles.Length + fsprojFiles.Length);
        allProjectFiles.AddRange(csprojFiles);
        allProjectFiles.AddRange(fsprojFiles);
        return allProjectFiles;
    }
}
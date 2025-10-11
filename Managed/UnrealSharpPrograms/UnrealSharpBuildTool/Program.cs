﻿using System.Xml.Linq;
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
            Console.WriteLine(">>> UnrealSharpBuildTool");
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
            
            if (!BuildToolAction.InitializeAction())
            {
                return 1;
            }

            Console.WriteLine($"UnrealSharpBuildTool executed {BuildToolOptions.Action.ToString()} action successfully.");
        }
        catch (Exception exception)
        {
            Console.WriteLine("An error occurred: " + exception.Message + Environment.NewLine + exception.StackTrace);
            return 1;
        }

        return 0;
    }

    public static string TryGetArgument(string argument)
    {
        return BuildToolOptions.TryGetArgument(argument);
    }

    public static IEnumerable<string> GetArguments(string argument)
    {
        return BuildToolOptions.GetArguments(argument);
    }

    public static bool HasArgument(string argument)
    {
        return BuildToolOptions.HasArgument(argument);
    }

    public static string GetSolutionFile()
    {
        return Path.Combine(GetScriptFolder(), BuildToolOptions.ProjectName + ".sln");
    }

    public static string GetUProjectFilePath()
    {
        return Path.Combine(BuildToolOptions.ProjectDirectory, BuildToolOptions.ProjectName + ".uproject");
    }

    public static string GetBuildConfiguration()
    {
        string buildConfig = TryGetArgument("BuildConfig");
        if (string.IsNullOrEmpty(buildConfig))
        {
            buildConfig = "Debug";
        }
        return buildConfig;
    }

    public static BuildConfig GetBuildConfig()
    {
        string buildConfig = GetBuildConfiguration();
        Enum.TryParse(buildConfig, out BuildConfig config);
        return config;
    }

    public static string GetBuildConfiguration(BuildConfig buildConfig)
    {
        return buildConfig switch
        {
            BuildConfig.Debug => "Debug",
            BuildConfig.Release => "Release",
            BuildConfig.Publish => "Release",
            _ => "Release"
        };
    }

    public static string GetScriptFolder()
    {
        return Path.Combine(BuildToolOptions.ProjectDirectory, "Script");
    }

    public static string GetPluginsFolder()
    {
        return Path.Combine(BuildToolOptions.ProjectDirectory, "Plugins");
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

        return Path.Combine(rootDir, "Binaries", "Managed", GetVersion());
    }

    public static string GetWeaver()
    {
        return Path.Combine(GetManagedBinariesDirectory(), "UnrealSharpWeaver.dll");
    }

    public static string GetManagedBinariesDirectory()
    {
        return Path.Combine(BuildToolOptions.PluginDirectory, "Binaries", "Managed", GetVersion());
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
            executablePath = Path.Combine(BuildToolOptions.EngineDirectory, "Binaries", "Win64", "UnrealEditor.exe");
        }
        else if (OperatingSystem.IsMacOS())
        {
            executablePath = Path.Combine(BuildToolOptions.EngineDirectory, "Binaries", "Mac", "UnrealEditor");
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
        return folder.GetDirectories("Script")
                .SelectMany(GetProjectsInDirectory)
                .Concat(folder.GetDirectories("Plugins")
                        .SelectMany(x => x.EnumerateFiles("*.uplugin", SearchOption.AllDirectories))
                        .Select(x => x.Directory)
                        .Select(x => x!.GetDirectories("Script").FirstOrDefault())
                        .Where(x => x is not null)
                        .SelectMany(GetProjectsInDirectory!))
                .ToList();
    }

    public static Dictionary<string, List<FileInfo>> GetProjectFilesByDirectory(DirectoryInfo folder)
    {
        Dictionary<string, List<FileInfo>> result = new Dictionary<string, List<FileInfo>>();
        DirectoryInfo? scriptsFolder = folder.GetDirectories("Script").FirstOrDefault();
        
        if (scriptsFolder is not null)
        {
            result.Add(GetOutputPathForDirectory(scriptsFolder), GetProjectsInDirectory(scriptsFolder).ToList());
        }

        foreach (DirectoryInfo? pluginFolder in folder.GetDirectories("Plugins")
                         .SelectMany(x => x.EnumerateFiles("*.uplugin", SearchOption.AllDirectories))
                         .Select(x => x.Directory)
                         .Select(x => x!.GetDirectories("Script").FirstOrDefault())
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
        var csprojFiles = folder.EnumerateFiles("*.csproj", SearchOption.AllDirectories);
        var fsprojFiles = folder.EnumerateFiles("*.fsproj", SearchOption.AllDirectories);
        return csprojFiles.Concat(fsprojFiles).Where(IsWeavableProject);
    }

    private static bool IsWeavableProject(FileInfo projectFile)
    {
        // We need to be able to filter out certain non-production projects.
        // The main target of this is source generators and analyzers which users
        // may want to leverage as part of their solution and can't be weaved because
        // they have to use netstandard2.0.
        XDocument doc = XDocument.Load(projectFile.FullName);
        return !doc.Descendants()
            .Where(element => element.Name.LocalName == "PropertyGroup")
            .SelectMany(element => element.Elements())
            .Any(element => element.Name.LocalName == "ExcludeFromWeaver" &&
                            element.Value.Equals("true", StringComparison.OrdinalIgnoreCase));
    }
}

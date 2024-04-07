using System.Reflection.PortableExecutable;
using CommandLine;
using UnrealSharpBuildTool.Actions;

namespace UnrealSharpBuildTool;

public static class Program
{
    public static BuildToolOptions buildToolOptions;

    public static int Main(string[] args)
    {
        try
        {
            Parser parser = new Parser(with => with.HelpWriter = null);
            ParserResult<BuildToolOptions> result = parser.ParseArguments<BuildToolOptions>(args);
        
            if (result.Tag == ParserResultType.NotParsed)
            {
                BuildToolOptions.PrintHelp(result);
                throw new Exception("Invalid arguments.");
            }
        
            buildToolOptions = result.Value;
            
            if (!BuildToolAction.InitializeAction())
            {
                throw new Exception("Failed to initialize action.");
            }
            
            Console.WriteLine($"UnrealSharpBuildTool executed {buildToolOptions.Action.ToString()} action successfully.");
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception.Message);
            return 1;
        }
        
        return 0;
    }

    public static string GetCSProjectFile()
    {
        return buildToolOptions.ProjectName + ".sln";
    }

    public static string GetUProjectFilePath()
    {
        return Path.Combine(buildToolOptions.ProjectDirectory, buildToolOptions.ProjectName + ".uproject");
    }
    
    public static string GetScriptFolderBinaries()
    {
        string currentBuildConfig = GetBuildConfiguration(buildToolOptions.BuildConfig);
        return Path.Combine(GetScriptFolder(), "bin", currentBuildConfig, GetVersion());
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
        return Path.Combine(buildToolOptions.ProjectDirectory, "Script");
    }
    
    public static string GetProjectDirectory()
    {
        return buildToolOptions.ProjectDirectory;
    }
    
    public static string FixPath(string path)
    {
        return path.Replace('/', '\\');
    }

    public static string GetProjectNameAsManaged()
    {
        return "Managed" + buildToolOptions.ProjectName;
    }
    
    public static string GetOutputPath()
    {
        string rootOutput = buildToolOptions.ArchiveDirectory ?? buildToolOptions.ProjectDirectory;
        
        if (buildToolOptions.BuildConfig == BuildConfig.Publish)
        {
            rootOutput = Path.Combine(rootOutput, buildToolOptions.ProjectName);
        }
        
        return Path.Combine(rootOutput, "Binaries", "Managed");
    }

    public static string GetWeaver()
    {
        return Path.Combine(GetManagedBinariesDirectory(), "UnrealSharpWeaver.dll");
    }

    public static string GetManagedBinariesDirectory()
    {
        return Path.Combine(buildToolOptions.PluginDirectory, "Binaries", "Managed");
    }
    
    public static string GetVersion()
    {
        Version currentVersion = Environment.Version;
        string currentVersionStr = $"{currentVersion.Major}.{currentVersion.Minor}";
        return "net" + currentVersionStr;
    }
}
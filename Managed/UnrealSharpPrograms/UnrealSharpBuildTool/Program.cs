using CommandLine;
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
                throw new Exception("Failed to initialize action.");
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
    
    public static string GetVersion()
    {
        Version currentVersion = Environment.Version;
        string currentVersionStr = $"{currentVersion.Major}.{currentVersion.Minor}";
        return "net" + currentVersionStr;
    }
}
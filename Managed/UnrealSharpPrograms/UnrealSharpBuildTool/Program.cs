using System.Reflection.PortableExecutable;
using CommandLine;
using UnrealSharpBuildTool.Actions;

namespace UnrealSharpBuildTool;

public static class Program
{
    public static BuildToolOptions buildToolOptions;

    public static int Main(string[] args)
    {
        Parser parser = new Parser(with => with.HelpWriter = null);
        ParserResult<BuildToolOptions> result = parser.ParseArguments<BuildToolOptions>(args);
        
        if (result.Tag == ParserResultType.NotParsed)
        {
            BuildToolOptions.PrintHelp(result);
            return 1;
        }
        
        buildToolOptions = result.Value;
        
        try
        {
            if (!BuildToolAction.InitializeAction())
            {
                return 2;
            }
            
            Console.WriteLine($"UnrealSharpBuildTool executed {buildToolOptions.Action.ToString()} action successfully.");
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception.Message);
            return 3;
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

    public static string GetOutputPath()
    {
        return buildToolOptions.OutputPath;
    }
    
    public static string GetScriptFolderBinaries()
    {
        string currentBuildConfig = buildToolOptions.BuildConfig.ToString();
        string version = GetVersion();
        return Path.Combine(GetScriptFolder(), $"bin/{currentBuildConfig}/{version}");
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

    public static string GetWeaver()
    {
        return "UnrealSharpWeaver.exe";
    }
    
    public static string GetVersion()
    {
        Version currentVersion = Environment.Version;
        string currentVersionStr = $"{currentVersion.Major}.{currentVersion.Minor}";
        return "net" + currentVersionStr;
    }
}
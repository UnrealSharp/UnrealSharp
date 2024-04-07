using System.Reflection;
using CommandLine;
using CommandLine.Text;

namespace UnrealSharpBuildTool;

public enum BuildAction : int
{
    Build,
    Clean,
    GenerateProject,
    Rebuild,
    Weave,
    Publish,
}

public enum BuildConfig : int
{
    Debug,
    Release,
    Publish,
}

public class BuildToolOptions
{
    [Option("Action", Required = true, HelpText = "The action the build tool should process. Build / Clean / GenerateProjects")]
    public BuildAction Action { get; set; }
    
    [Option("DotNetPath", Required = false, HelpText = "The path to the dotnet.exe")]
    public string? DotNetExecutable { get; set; }
    
    [Option("BuildConfig", Required = false, HelpText = "Build with debug or release")]
    public BuildConfig BuildConfig { get; set; }
    
    [Option("ProjectDirectory", Required = true, HelpText = "The directory where the .uproject file resides.")]
    public string ProjectDirectory { get; set; }
    
    [Option("PluginDirectory", Required = false, HelpText = "The UnrealSharp plugin directory.")]
    public string PluginDirectory { get; set; }
    
    [Option("EngineDirectory", Required = false)]
    public string EngineDirectory { get; set; }
    
    [Option("ProjectName", Required = true, HelpText = "The name of the Unreal Engine project.")]
    public string ProjectName { get; set; }
    
    [Option("ArchiveDirectory", Required = false, HelpText = "The directory where the archive should be stored.")]
    public string? ArchiveDirectory { get; set; }

    public static void PrintHelp(ParserResult<BuildToolOptions> result)
    {
        string name = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
        Console.Error.WriteLine($"Usage: {name} [options]");
        Console.Error.WriteLine("Options:");

        var helpText = HelpText.AutoBuild(result, h => h, e => e);
        Console.WriteLine(helpText);
    }
}
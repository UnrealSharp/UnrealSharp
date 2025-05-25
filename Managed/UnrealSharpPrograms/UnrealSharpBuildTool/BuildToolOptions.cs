using System.Reflection;
using CommandLine;
using CommandLine.Text;

namespace UnrealSharpBuildTool;

public class BuildToolOptions
{
    [Option('a', "Action", Required = true, HelpText = "The action the build tool should process")]
    public string Action { get; set; }
    
    [Option("ActionArgs", Required = false, HelpText = "Additional arguments to pass to the action. Use the format 'key=value'.")]
    public IEnumerable<string> ActionArgs { get; set; } = new List<string>();
    
    [Option("DotNetPath", Required = false, HelpText = "The path to the dotnet.exe")]
    public string DotNetPath { get; set; } = string.Empty;
    
    [Option("ProjectDirectory", Required = true, HelpText = "The directory where the .uproject file resides.")]
    public string ProjectDirectory { get; set; } = string.Empty;
    
    [Option("PluginDirectory", Required = false, HelpText = "The UnrealSharp plugin directory.")]
    public string PluginDirectory { get; set; } = string.Empty;
    
    [Option("EngineDirectory", Required = false, HelpText = "The Unreal Engine directory.")]
    public string EngineDirectory { get; set; } = string.Empty;
    
    [Option("ProjectName", Required = true, HelpText = "The name of the Unreal Engine project.")]
    public string ProjectName { get; set; } = string.Empty;

    public static void PrintHelp(ParserResult<BuildToolOptions> result)
    {
        string name = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly()!.Location);
        Console.Error.WriteLine($"Usage: {name} [options]");
        Console.Error.WriteLine("Options:");

        HelpText? helpText = HelpText.AutoBuild(result, h => h, e => e);
        Console.WriteLine(helpText);
        
        ActionManager.PrintActions();
    }
}
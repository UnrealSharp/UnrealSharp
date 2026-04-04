using System.Reflection;
using CommandLine;
using CommandLine.Text;

namespace UnrealSharpBuildTool;

public class BuildToolOptions
{
    public static BuildToolOptions Instance = null!;
    
    [Option('a', "Action", Required = true, HelpText = "The action the build tool should process")]
    public required string Action { get; set; }
    
    [Option("ActionArgs", Required = false, HelpText = "Additional arguments to pass to the action. Use the format 'key=value'.")]
    public IEnumerable<string> ActionArgs { get; set; } = new List<string>();
    
    [Option("DotNetPath", Required = false, HelpText = "The path to the dotnet.exe")]
    public string DotNetPath { get; set; } = string.Empty;

    [Option("ProjectDirectory", Required = true, HelpText = "The directory where the .uproject file resides.")]
    public string ProjectDirectory { get; set; } = string.Empty;

    [Option("PluginDirectory", Required = true, HelpText = "The UnrealSharp plugin directory.")]
    public string PluginDirectory { get; set; } = string.Empty;

    [Option("EngineDirectory", Required = false, HelpText = "The Unreal Engine directory.")]
    public string EngineDirectory { get; set; } = string.Empty;

    [Option("ProjectName", Required = true, HelpText = "The name of the Unreal Engine project.")]
    public string ProjectName { get; set; } = string.Empty;
    
    public static void ParseArguments(string[] args)
    {
        Parser parser = new Parser(with => with.HelpWriter = null);
        ParserResult<BuildToolOptions> result = parser.ParseArguments<BuildToolOptions>(args);

        if (result.Tag == ParserResultType.NotParsed)
        {
            PrintHelp(result);

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
            
        Instance = result.Value;
    }

    private static void PrintHelp(ParserResult<BuildToolOptions> result)
    {
        string name = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly()!.Location);
        Console.Error.WriteLine($"Usage: {name} [options]");
        HelpText helpText = HelpText.AutoBuild(result, (HelpText h) => h, (Example e) => e);
        Console.WriteLine(helpText);
        
        ActionManager.PrintActions();
    }
}
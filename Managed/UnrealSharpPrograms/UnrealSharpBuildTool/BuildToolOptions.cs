using System.Reflection;
using CommandLine;
using CommandLine.Text;

namespace UnrealSharpBuildTool;

public enum BuildAction : int
{
    Build,
    Clean,
    GenerateProject,
    UpdateProjectDependencies,
    Rebuild,
    Weave,
    PackageProject,
    GenerateSolution,
    BuildWeave,
}

public enum BuildConfig : int
{
    Debug,
    Release,
    Publish,
}

public class BuildToolOptions
{
    [Option("Action", Required = true, HelpText = "The action the build tool should process. Possible values: Build, Clean, GenerateProject, Rebuild, Weave, PackageProject, GenerateSolution, BuildWeave.")]
    public BuildAction Action { get; set; }

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

    [Option("AdditionalArgs", Required = false, HelpText = "Additional key-value arguments for the build tool.")]
    public IEnumerable<string> AdditionalArgs { get; set; } = new List<string>();

    public string TryGetArgument(string argument)
    {
        return GetArguments(argument).FirstOrDefault() ?? string.Empty;
    }

    public IEnumerable<string> GetArguments(string argument)
    {
        return AdditionalArgs.Where(arg => arg.StartsWith(argument))
                .Select(arg => arg[(argument.Length + 1)..]);
    }

    public bool HasArgument(string argument)
    {
        return AdditionalArgs.Any(arg => arg.StartsWith(argument));
    }

    public static void PrintHelp(ParserResult<BuildToolOptions> result)
    {
        string name = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly()!.Location);
        Console.Error.WriteLine($"Usage: {name} [options]");
        Console.Error.WriteLine("Options:");

        var helpText = HelpText.AutoBuild(result, h => h, e => e);
        Console.WriteLine(helpText);
    }
}

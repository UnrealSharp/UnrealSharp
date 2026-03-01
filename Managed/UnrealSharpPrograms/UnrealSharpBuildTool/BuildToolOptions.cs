using System.Reflection;
using CommandLine;
using CommandLine.Text;

namespace UnrealSharpBuildTool;

public enum BuildAction
{
    GenerateProject,
    UpdateProjectDependencies,
    PackageProject,
    GenerateSolution,
    BuildEmitLoadOrder,
    MergeSolution
}

public enum BuildConfig
{
    Debug,
    Release,
    Publish,
}

public class BuildToolOptions
{
    [Option("Action", Required = true, HelpText = "The action the build tool should process.")]
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

    [Option("AdditionalArgs", Required = false, HelpText = "Additional key-value arguments.")]
    public IEnumerable<string> AdditionalArgs { get; set; } = new List<string>();

    public string GetArgument(string argument, string defaultValue = "")
    {
        string? result = AdditionalArgs.FirstOrDefault(arg => arg.StartsWith(argument));
        
        if (result == null)
        {
            return defaultValue;
        }
        
        return result[(argument.Length + 1)..];
    }

    public bool GetArgumentBool(string argument, bool defaultValue = false)
    {
        string value = GetArgument(argument);
        
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        if (bool.TryParse(value, out bool result))
        {
            return result;
        }
        
        return defaultValue;
    }

    public int GetArgumentInt(string argument, int defaultValue = 0)
    {
        string value = GetArgument(argument);
        
        if (int.TryParse(value, out int result))
        {
            return result;
        }
        
        return defaultValue;
    }

    public IEnumerable<string> GetArguments(string argument)
    {
        return AdditionalArgs
            .Where(arg => arg.StartsWith(argument))
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
        HelpText helpText = HelpText.AutoBuild(result, (HelpText h) => h, (Example e) => e);
        Console.WriteLine(helpText);
    }
}
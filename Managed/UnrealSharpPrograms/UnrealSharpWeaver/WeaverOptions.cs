using System.Reflection;
using CommandLine;
using CommandLine.Text;

namespace UnrealSharpWeaver;

public class WeaverOptions
{
    [Option('p', "path", Required = true, HelpText = "Search paths for assemblies.")]
    public required IEnumerable<string> AssemblyPaths { get; set; }

    [Option('o', "output", Required = true, HelpText = "DLL output directory.")]
    public required string OutputDirectory { get; set; }

    public WeaverOptions(IEnumerable<string> assemblyPaths, string outputDirectory)
    {
        AssemblyPaths = assemblyPaths;
        OutputDirectory = outputDirectory;
    }

    public WeaverOptions() : this([], string.Empty)
    {
    }

    private static void PrintHelp(ParserResult<WeaverOptions> result)
    {
        if (result.Tag != ParserResultType.NotParsed)
        {
            return;
        }

        string name = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly()!.Location);
        Console.Error.WriteLine($"Usage: {name}");
        Console.Error.WriteLine("Commands: ");

        var helpText = HelpText.AutoBuild(result, h => h, e => e);
        Console.WriteLine(helpText);
    }

    public static WeaverOptions ParseArguments(IEnumerable<string> args)
    {
        Parser parser = new Parser(settings =>
        {
            settings.AllowMultiInstance = true;
            settings.HelpWriter = null;
        });

        ParserResult<WeaverOptions> result = parser.ParseArguments<WeaverOptions>(args);

        if (result.Tag != ParserResultType.NotParsed)
        {
            return result.Value;
        }

        PrintHelp(result);
        throw new InvalidOperationException("Invalid arguments.");
    }
}

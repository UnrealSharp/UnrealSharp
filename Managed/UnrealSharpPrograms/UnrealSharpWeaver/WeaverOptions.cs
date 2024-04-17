using System.Reflection;
using CommandLine;
using CommandLine.Text;
using Mono.Cecil;

namespace UnrealSharpWeaver;

public class WeaverOptions
{
    [Option('p', "path", Required = false, HelpText = "Search paths for assemblies.")]
    public IEnumerable<string> AssemblyPaths { get; set; }

    [Option('o', "output", Required = false, HelpText = "DLL output directory.")]
    public string OutputDirectory { get; set; }
    
    [Option('n', "projectname", Required = false, HelpText = "DLL output directory.")]
    public string ProjectName { get; set; }

    private static void PrintHelp(ParserResult<WeaverOptions> result)
    {
        if (result.Tag == ParserResultType.NotParsed)
        {
            string name = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
            Console.Error.WriteLine($"Usage: {name}");
            Console.Error.WriteLine("Commands: ");
        
            var helpText = HelpText.AutoBuild(result, h => h, e => e);
            Console.WriteLine(helpText);
        }
    }

    public static WeaverOptions ParseArguments(IEnumerable<string> args)
    {
        Parser parser = new Parser(with => with.HelpWriter = null);
        ParserResult<WeaverOptions> result = parser.ParseArguments<WeaverOptions>(args);

        if (result.Tag != ParserResultType.NotParsed)
        {
            return result.Value;
        }
        
        PrintHelp(result);
        throw new InvalidOperationException("Invalid arguments.");
    }
}
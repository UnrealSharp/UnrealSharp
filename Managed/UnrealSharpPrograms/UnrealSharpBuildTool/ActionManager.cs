using System.Reflection;
using CommandLine;
using CommandLine.Text;
using UnrealSharpBuildTool.Exceptions;

namespace UnrealSharpBuildTool;

public static class ActionManager
{
    private static readonly IEnumerable<Type> VerbTypesCache = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute<VerbAttribute>() != null);
    
    public static string[] TransformArgs(string actionName, string[] actionArgs)
    {
        var groupedArgs = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        List<string> positionalArgs = new List<string>();

        foreach (string argument in actionArgs)
        {
            if (argument.Contains('='))
            {
                string[] parts = argument.Split('=', 2);
                string key = parts[0].TrimStart('-');
                string value = parts[1];

                if (!groupedArgs.ContainsKey(key))
                {
                    groupedArgs[key] = new List<string>();
                }
                groupedArgs[key].Add(value);
            }
            else
            {
                positionalArgs.Add(argument);
            }
        }
        
        List<string> processed = new List<string>();
        processed.Add(actionName);
        
        foreach (var kvp in groupedArgs)
        {
            processed.Add($"--{kvp.Key}");
            processed.AddRange(kvp.Value);
        }
        
        processed.AddRange(positionalArgs);

        return processed.ToArray();
    }
    
    public static string GetMatchingVerb(string actionName)
    {
        foreach (Type verbType in VerbTypesCache)
        {
            VerbAttribute verb = verbType.GetCustomAttribute<VerbAttribute>()!;
            
            if (!verb.Aliases.Any(alias => string.Equals(alias, actionName, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }
            
            return verb.Name;
        }

        throw new Exception($"No matching verb found for action '{actionName}'.");
    }
    
    public static void RunAction(string action, string[] actionArguments)
    {
        string actionAlias = GetMatchingVerb(action);
        string[] transformedActionArguments = TransformArgs(actionAlias, actionArguments);
    
        ParserResult<object>? result = Parser.Default.ParseArguments(transformedActionArguments, VerbTypesCache.ToArray());

        result.WithParsed(options =>
        {
            Type optionType = options.GetType();
            MethodInfo? handler = Assembly.GetExecutingAssembly().GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                .FirstOrDefault(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == optionType);

            if (handler != null)
            {
                try 
                {
                    handler.Invoke(null, [options]);
                }
                catch (TargetInvocationException exception)
                {
                    throw new BuildToolException($"Action '{action}' failed: {exception.InnerException?.Message}", exception.InnerException!);
                }
            }
            else
            {
                throw new BuildToolException($"No handler defined for option type '{optionType.Name}'.");
            }
        });

        result.WithNotParsed(errors =>
        {
            HelpText? errorMessages = HelpText.DefaultParsingErrorsHandler(result, HelpText.AutoBuild(result, h => h, e => e));
            throw new BuildToolException($"Failed to parse arguments for action '{action}'. Errors: {errorMessages}");
        });
    }
    
    public static void PrintActions()
    {
        Console.WriteLine("Available Actions:");
        Console.WriteLine("========================================");
        Console.WriteLine();

        foreach (Type verbType in VerbTypesCache)
        {
            VerbAttribute verb = verbType.GetCustomAttribute<VerbAttribute>()!;
            Console.WriteLine($"Action: {verb.Name}");
            Console.WriteLine($"Description: {verb.HelpText}");
            Console.WriteLine("Options:");
            RecursivePrintOptions(verbType, indentLevel: 1);
            Console.WriteLine();
        }

        Console.WriteLine("========================================");
    }

    private static void RecursivePrintOptions(Type type, int indentLevel)
    {
        string indent = new string(' ', indentLevel * 2);

        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            OptionAttribute? option = property.GetCustomAttribute<OptionAttribute>();
            if (option == null)
            {
                continue;
            }

            string shortName = string.IsNullOrEmpty(option.ShortName) ? string.Empty : $" (-{option.ShortName})";
            Console.WriteLine($"{indent}--{option.LongName}{shortName}: {option.HelpText} (Required: {option.Required})");
        }
    }
}
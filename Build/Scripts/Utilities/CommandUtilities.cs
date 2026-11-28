using System;
using System.Collections.Generic;
using System.Reflection;
using AutomationTool;

namespace UnrealSharp.Automation.Utilities;

public static class CommandUtilities
{
    public static bool RunCommand(string commandName, string projectPath, List<KeyValuePair<string, string>>? commandArguments = null)
    {
        // UHT also uses this assembly, but does not load UAT's BuildCookRun dependencies.
        // Resolve only the requested command instead of loading every command type.
        Type? CommandType = Assembly.GetExecutingAssembly().GetType($"UnrealSharp.Automation.BuildCommands.{commandName}");
        if (CommandType == null || !typeof(BuildCommand).IsAssignableFrom(CommandType))
        {
            throw new Exception($"Failed to find and execute UnrealSharp automation action '{commandName}'.");
        }

        BuildCommand CommandInstance = (BuildCommand)Activator.CreateInstance(CommandType)!;

        List<string> Arguments = new List<string>();
        Arguments.Add($"Project={projectPath}");

        if (commandArguments != null)
        {
            foreach (KeyValuePair<string, string> Argument in commandArguments)
            {
                Arguments.Add($"{Argument.Key}={Argument.Value}");
            }
        }

        CommandInstance.Params = Arguments.ToArray();
        return CommandInstance.Execute() == 0;
    }
    
    internal static bool RunCommand(string commandName, BuildCommand commandCaller, List<KeyValuePair<string, string>>? commandArguments = null)
    {
        return RunCommand(commandName, commandCaller.ParseRequiredStringParam("Project"), commandArguments);
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using AutomationTool;

namespace UnrealSharp.Automation.Utilities;

public static class CommandUtilities
{
    public static bool RunCommand(string commandName, string projectPath, List<KeyValuePair<string, string>>? commandArguments = null)
    {
        foreach (Type Type in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (Type.Name != commandName)
            {
                continue;
            }
            
            BuildCommand CommandInstance = (BuildCommand)Activator.CreateInstance(Type)!;

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
        
        throw new Exception($"Failed to find and execute UnrealSharp automation action '{commandName}'.");
    }
    
    internal static bool RunCommand(string commandName, BuildCommand commandCaller, List<KeyValuePair<string, string>>? commandArguments = null)
    {
        return RunCommand(commandName, commandCaller.ParseRequiredStringParam("Project"), commandArguments);
    }
}
using System;
using System.Collections.Generic;
using System.Reflection;
using AutomationTool;

namespace UnrealSharp.Automation.Utilities;

public static class CommandUtilities
{
    public static bool RunCommand(string commandName, string projectPath, List<KeyValuePair<string, string>>? actionArgs = null)
    {
        foreach (Type Type in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (Type.Name != commandName)
            {
                continue;
            }
            
            BuildCommand CommandInstance = (BuildCommand)Activator.CreateInstance(Type)!;

            List<string> args = new List<string>();
            args.Add($"Project={projectPath}");
            
            if (actionArgs != null)
            {
                foreach (KeyValuePair<string, string> arg in actionArgs)
                {
                    args.Add($"{arg.Key}={arg.Value}");
                }
            }
            
            CommandInstance.Params = args.ToArray();
            return CommandInstance.Execute() == 0;
        }
        
        throw new Exception($"Failed to find and execute UnrealSharp automation action '{commandName}'.");
    }
    
    internal static bool RunCommand(string commandName, BuildCommand caller, List<KeyValuePair<string, string>>? actionArgs = null)
    {
        return RunCommand(commandName, caller.ParseRequiredStringParam("Project"), actionArgs);
    }
}
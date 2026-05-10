using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using UnrealSharp.Shared;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue;

public static class USharpBuildToolUtilities
{
    public static bool InvokeUnrealSharpAutomation(string action, List<KeyValuePair<string, string>>? actionArgs = null)
    {
        string path = Path.Combine(GeneratorStatics.EngineDirectory, "Binaries", "DotNET", "AutomationTool", "AutomationTool.dll");
        string automationPath = Path.Combine(GeneratorStatics.PluginDirectory, "Build", "Scripts");
        
        if (!File.Exists(path))
        {
            Console.WriteLine($"Failed to find Unreal Automation Tool at path: {path}");
            return false;
        }
        
        Collection<string> argumentList = new Collection<string>();
        argumentList.Add(path);
        argumentList.Add($"{action}");
        argumentList.Add($"-ScriptDir={automationPath}");
        argumentList.Add($"-Project={GeneratorStatics.Factory.Session.ProjectFile}");
        
        if (actionArgs != null)
        {
            foreach (KeyValuePair<string, string> arg in actionArgs)
            {
                argumentList.Add($"-{arg.Key}={arg.Value}");
            }
        }

        DotNetUtilities.InvokeDotNet(argumentList);
        return true;
    }
}
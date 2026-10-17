using System.Collections.Generic;
using System.IO;
using UnrealBuildTool;

namespace UnrealSharpManagedGlue.Utilities;

public static class BuildUtilities
{
    public static void BuildBindings()
    {
        if (SessionStatic.Generator.TargetType != TargetRules.TargetType.Editor || !SessionStatic.Files.HasModifiedEngineGlue)
        {
            return;
        }
        
        ConsoleUtilities.Log("Engine glue has been modified since the last build. Rebuilding UnrealSharp bindings...");

        List<KeyValuePair<string, string>> actionArgs =
        [
            new("Folders", Path.Combine(SessionStatic.Generator.ManagedPath, "UnrealSharp")),
            new("TargetConfiguration", SessionStatic.Generator.TargetConfiguration.ToString())
        ];
        
        UnrealSharpAutomationUtilities.InvokeUnrealSharpAutomation("BuildSolution", actionArgs);
    }
    
    public static void GenerateUserSolution()
    {
        if (SessionStatic.Generator.TargetType != TargetRules.TargetType.Editor)
        {
            return;
        }
        
        List<KeyValuePair<string, string>> ActionArgs =
        [
            new("TargetType", SessionStatic.Generator.TargetType.ToString()),
            new("TargetConfiguration", SessionStatic.Generator.TargetConfiguration.ToString())
        ];
        UnrealSharpAutomationUtilities.InvokeUnrealSharpAutomation("GenerateUserSolution", ActionArgs);
    }
}

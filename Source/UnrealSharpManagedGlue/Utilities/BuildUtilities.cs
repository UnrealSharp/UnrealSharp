using System.Collections.Generic;
using System.IO;
using UnrealBuildTool;

namespace UnrealSharpManagedGlue.Utilities;

public static class BuildUtilities
{
    public static void BuildBindings()
    {
        if (GeneratorStatics.TargetType != TargetRules.TargetType.Editor || !FileExporter.HasModifiedEngineGlue)
        {
            return;
        }
        
        ConsoleUtilities.Log("Engine glue has been modified since the last build. Rebuilding bindings...");

        List<KeyValuePair<string, string>> actionArgs =
        [
            new("Folders", Path.Combine(GeneratorStatics.ManagedPath, "UnrealSharp")),
            new("TargetConfiguration", GeneratorStatics.TargetConfiguration.ToString())
        ];
        
        UnrealSharpAutomationUtilities.InvokeUnrealSharpAutomation("BuildSolution", actionArgs);
    }
    
    public static void GenerateUserSolution()
    {
        UnrealSharpAutomationUtilities.InvokeUnrealSharpAutomation("GenerateUserSolution");
    }
}
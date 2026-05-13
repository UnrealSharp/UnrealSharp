using System.Collections.Generic;
using System.IO;
using UnrealBuildTool;
using UnrealSharp.Shared;

namespace UnrealSharpManagedGlue.Utilities;

public static class BuildUtilities
{
    public static void BuildBindings()
    {
        if (GeneratorStatics.BuildTarget != TargetRules.TargetType.Editor || !FileExporter.HasModifiedEngineGlue)
        {
            return;
        }
        
        ConsoleUtilities.Log("Engine glue has been modified since the last build. Rebuilding bindings...");

        List<KeyValuePair<string, string>> actionArgs = new List<KeyValuePair<string, string>>();
        actionArgs.Add(new KeyValuePair<string, string>("Folders", Path.Combine(GeneratorStatics.ManagedPath, "UnrealSharp")));
        actionArgs.Add(new KeyValuePair<string, string>("BuildConfig", GeneratorStatics.BuildConfiguration.ToString()));
        UnrealSharpAutomationUtilities.InvokeUnrealSharpAutomation("BuildSolution", actionArgs);
    }
}
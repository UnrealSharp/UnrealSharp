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
        DotNetUtilities.BuildSolution(Path.Combine(GeneratorStatics.ManagedPath, "UnrealSharp"));
    }
}
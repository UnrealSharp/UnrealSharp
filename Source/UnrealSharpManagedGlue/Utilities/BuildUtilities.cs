using System;
using System.Collections.ObjectModel;
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
        string projectRootDirectory = Path.Combine(GeneratorStatics.ManagedPath, "UnrealSharp");
        
        if (!Directory.Exists(projectRootDirectory))
        {
            throw new Exception($"Couldn't find project root directory: {projectRootDirectory}");
        }

        Collection<string> arguments = new Collection<string> { "build" };
        DotNetUtilities.InvokeDotNet(arguments, projectRootDirectory);
    }
}
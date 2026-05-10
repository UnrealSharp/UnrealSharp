using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using UnrealSharp.Shared;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue;

public static class USharpBuildToolUtilities
{
    public static bool InvokeUSharpBuildTool(string action, List<KeyValuePair<string, string>>? actionArgs = null)
    {
        string path = Path.Combine(GeneratorStatics.ManagedBinariesPath, DotNetUtilities.DOTNET_MAJOR_VERSION_DISPLAY);
        return DotNetUtilities.InvokeUSharpBuildTool(action, path,
            GeneratorStatics.ProjectName,
            GeneratorStatics.PluginDirectory,
            GeneratorStatics.Factory.Session.ProjectDirectory!,
            GeneratorStatics.Factory.Session.EngineDirectory!,
            actionArgs);
    }

    public static void CompileUSharpBuildTool()
    {
        ConsoleUtilities.Log("Compiling USharpBuildTool...");
        
        string uSharpBuildToolDirectory = Path.Combine(GeneratorStatics.ManagedPath, "UnrealSharpPrograms");
        
        if (!Directory.Exists(uSharpBuildToolDirectory))
        {
            throw new DirectoryNotFoundException($"Failed to find UnrealSharpPrograms directory at: {uSharpBuildToolDirectory}");
        }
        
        Collection<string> arguments = new Collection<string>
        {
            "build",
        };
        
        if (!DotNetUtilities.InvokeDotNet(arguments, uSharpBuildToolDirectory))
        {
            throw new InvalidOperationException("Failed to compile USharpBuildTool.");
        }
    }
}
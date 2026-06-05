using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnrealSharp.Shared;
using AssemblyUtilities = UnrealSharp.Shared.AssemblyUtilities;

namespace UnrealSharp.Automation.Utilities;

public static class LoadOrderUtilities
{
    public const string UserLoadOrderName = "UserCode";
    public const int UserLoadOrderPriority = 0;

    public const string GlueLoadOrderName = "GlueCode";
    public const int GlueLoadOrderPriority = 100;
    
    public static List<string> ResolveAssemblyPaths(IEnumerable<string> projectFilesOrNames, string outputPath)
    {
        List<string> AssemblyPaths = new List<string>();

        foreach (string ProjectFileOrName in projectFilesOrNames)
        {
            string AssemblyName = Path.GetFileNameWithoutExtension(ProjectFileOrName);
            string AssemblyPath = Path.Combine(outputPath, AssemblyName + ".dll");

            if (!File.Exists(AssemblyPath))
            {
                LoggerUtilities.LogUnrealSharpWarning($"Could not find assembly for project {AssemblyName} at expected path {AssemblyPath}. Skipping.");
                continue;
            }

            AssemblyPaths.Add(AssemblyPath);
        }

        return AssemblyPaths;
    }
    
    public static void TryEmitLoadOrder(IEnumerable<string> projectFilesOrNames, string outputPath, string loadOrderName, LoadOrderOptions options)
    {
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        List<string> ProjectList = projectFilesOrNames.ToList();

        LoggerUtilities.LogUnrealSharpInfo($"Emitting assembly load order '{loadOrderName}' for assemblies: {string.Join(", ", ProjectList.Select(file => Path.GetFileNameWithoutExtension(file)))}");

        List<string> AssemblyPaths = ResolveAssemblyPaths(ProjectList, outputPath);

        if (AssemblyPaths.Count == 0)
        {
            LoggerUtilities.LogUnrealSharpWarning($"No assemblies could be resolved for load order '{loadOrderName}'. Skipping load order emission.");
            return;
        }

        AssemblyUtilities.EmitLoadOrder(AssemblyPaths, outputPath, options, loadOrderName);
    }
}
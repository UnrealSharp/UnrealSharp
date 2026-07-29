using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using UnrealSharpManagedGlue.SourceGeneration;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue.Exporters;

public static class PreprocessorExporter
{
    public static void ExportBuildDefines()
    {
        GenerateMSBuildProps(ParseBuildRulesProject(GeneratorStatics.EngineDirectory));
    }
    
    private static HashSet<string> ParseBuildRulesProject(string engineDirectory)
    {
        HashSet<string> definesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        string csproj = Path.Combine(engineDirectory, "Intermediate", "Build", "BuildRulesProjects", "UE5Rules", "UE5Rules.csproj");
        if (File.Exists(csproj))
        {
            try
            {
                XDocument document = XDocument.Load(csproj);
                IEnumerable<string> values = document.Descendants("DefineConstants").Select(x => x.Value);
                foreach (string value in values)
                {
                    foreach (string raw in value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string s = raw.Trim();

                        if (s.Length == 0 || s.StartsWith("$(", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        definesSet.Add(s);
                    }
                }
            }
            catch
            {
                // Installed engine builds may omit or restrict access to this generated project.
            }
        }

        AddEngineVersionDefines(engineDirectory, definesSet);
        return definesSet;
    }

    private static void AddEngineVersionDefines(string engineDirectory, HashSet<string> definesSet)
    {
        string buildVersionPath = Path.Combine(engineDirectory, "Build", "Build.version");
        if (!File.Exists(buildVersionPath))
        {
            return;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(buildVersionPath));
            int engineMajor = document.RootElement.GetProperty("MajorVersion").GetInt32();
            int engineMinor = document.RootElement.GetProperty("MinorVersion").GetInt32();

            // Match UnrealBuildTool's UE_X_Y_OR_LATER convention.
            for (int major = 4; major <= engineMajor; major++)
            {
                int firstMinor = major == 4 ? 17 : 0;
                int lastMinor = major == engineMajor ? engineMinor : 30;
                for (int minor = firstMinor; minor <= lastMinor; minor++)
                {
                    definesSet.Add($"UE_{major}_{minor}_OR_LATER");
                }
            }
        }
        catch
        {
            // Keep defines parsed from UE5Rules.csproj if Build.version is unavailable or invalid.
        }
    }

    private static void GenerateMSBuildProps(HashSet<string> defines)
    {
        IOrderedEnumerable<string> ordered = defines
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s);

        string joined = string.Join(";", ordered);

        GeneratorStringBuilder stringBuilder = new GeneratorStringBuilder();

        stringBuilder.AppendLine("<Project>");
        stringBuilder.Indent();
        stringBuilder.AppendLine("<PropertyGroup>");
        stringBuilder.Indent();
        stringBuilder.AppendLine($"<DefineConstants>$(DefineConstants);{joined}</DefineConstants>");
        stringBuilder.UnIndent();
        stringBuilder.AppendLine("</PropertyGroup>");
        stringBuilder.UnIndent();
        stringBuilder.AppendLine("</Project>");

        string propsPath = Path.Combine(GeneratorStatics.PluginModuleInfo.Module.GetUHTBaseDirectory(), "UE5Rules.Defines.props");
        File.WriteAllText(propsPath, stringBuilder.ToString());
    }
}

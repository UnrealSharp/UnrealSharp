using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnrealSharpManagedGlue.SourceGeneration;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue.Exporters;

public static class PreprocessorExporter
{
    private static HashSet<string> LoadUE5RulesDefines(string engineDirectory)
    {
        HashSet<string> definesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string csproj = Path.Combine(engineDirectory, "Intermediate", "Build", "BuildRulesProjects", "UE5Rules", "UE5Rules.csproj");

        if (!File.Exists(csproj))
        {
            return definesSet;
        }

        XDocument doc;
        try 
        { 
            doc = XDocument.Load(csproj); 
        }
        catch 
        {
            return definesSet; 
        }

        IEnumerable<string> values = doc.Descendants("DefineConstants").Select(x => x.Value);

        foreach (string value in values)
        {
            foreach (string raw in value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string s = raw.Trim();
                
                if (s.Length == 0)
                {
                    continue;
                }

                if (s.StartsWith("$(", StringComparison.Ordinal))
                {
                    continue;
                }

                definesSet.Add(s);
            }
        }

        return definesSet;
    }
    
    public static void StartExportingPreprocessors()
    {
        TaskManager.StartTask(static _ =>
        {
            ExportDirective(LoadUE5RulesDefines(GeneratorStatics.EngineDirectory));
        });
    }

    private static void ExportDirective(HashSet<string> defines)
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

        string propsPath = Path.Combine(GeneratorStatics.EngineGluePath, "UE5Rules.Defines.props");
        File.WriteAllText(propsPath, stringBuilder.ToString());
    }

}

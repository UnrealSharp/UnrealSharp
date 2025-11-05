using EpicGames.UHT.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnrealSharpScriptGenerator.Utilities;
using static System.Net.Mime.MediaTypeNames;

namespace UnrealSharpScriptGenerator.Exporters;

public static class PreprocessorExporter
{

    private static HashSet<string> LoadUE5RulesDefines(string engineDir)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var csproj = Path.Combine(
            engineDir,
            "Intermediate", "Build", "BuildRulesProjects", "UE5Rules", "UE5Rules.csproj");

        if (!File.Exists(csproj))
        {
            return set;
        }

        XDocument doc;
        try 
        { 
            doc = XDocument.Load(csproj); 
        }
        catch 
        {
            return set; 
        }

        var values = doc.Descendants("DefineConstants")
            .Select(x => x.Value ?? string.Empty);

        foreach (var val in values)
        {
            foreach (var raw in val.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var s = raw.Trim();
                if (s.Length == 0) continue;

                if (s.StartsWith("$(", StringComparison.Ordinal)) continue;

                set.Add(s);
            }
        }

        return set;
    }


    public static void StartExportingPreprocessors(string? engineDirectory, List<Task> tasks)
    {
        if (engineDirectory == null)
        {
            throw new InvalidOperationException("Engine directory is null, cannot load UE5Rules defines.");
        }

        tasks.Add(Program.Factory.CreateTask(_ =>
        {
            ExportDirective(LoadUE5RulesDefines(engineDirectory));
        })!);
    }

    private static void ExportDirective(HashSet<string> defines)
    {
        var ordered = defines
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s);

        var joined = string.Join(";", ordered);

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

        var propsPath = Path.Combine(Program.EngineGluePath, "UE5Rules.Defines.props");
        File.WriteAllText(propsPath, stringBuilder.ToString());
    }

}

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
        GeneratorStringBuilder stringBuilder = new();

        foreach(string define in defines)
        {
            stringBuilder.DeclarePreprocessor(define);
        }

        var definesFilePath = Path.Combine(Program.EngineGluePath, "UE5RulesDefines.cs");

        File.WriteAllText(definesFilePath, stringBuilder.ToString());
    }

}

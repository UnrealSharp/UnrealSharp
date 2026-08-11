using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnrealBuildTool;
using UnrealSharpManagedGlue.SourceGeneration;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue.Exporters;

public static class PreprocessorExporter
{
    public static void GenerateMSBuildProps()
    {
        string joined = string.Join(";", RulesAssembly.GetPreprocessorDefinitions());
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
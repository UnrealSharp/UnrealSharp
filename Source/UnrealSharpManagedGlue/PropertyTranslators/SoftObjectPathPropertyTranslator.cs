using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.SourceGeneration;

namespace UnrealSharpManagedGlue.PropertyTranslators;

// Soft paths have an asset-path text representation, not a reflected field tuple.
public class SoftObjectPathPropertyTranslator : StructPropertyTranslator
{
    public override bool CanExport(UhtProperty property)
        => property is UhtStructProperty structure && structure.ScriptStruct.SourceName is "FSoftObjectPath" or "FSoftClassPath";

    public override bool CanExportDefaultValue(UhtFunction function, UhtProperty property, string value)
        => value == "()" || TryPath(property, value, out _);

    internal bool TryPath(UhtProperty property, string value, out string expression)
    {
        expression = "";
        // Subobject and export-text paths require different construction semantics.
        // Preserve the explicit API for those until a matching translator is provided.
        if (!Regex.IsMatch(value, @"\A/[A-Za-z0-9_/]+\.[A-Za-z0-9_]+\z")) return false;
        int separator = value.LastIndexOf('.');
        string package = JsonSerializer.Serialize(value.Substring(0, separator));
        string asset = JsonSerializer.Serialize(value.Substring(separator + 1));
        expression = $"new {GetManagedType(property)}(new global::UnrealSharp.CoreUObject.FTopLevelAssetPath(new FName({package}), new FName({asset})))";
        return true;
    }

    public override void ExportCppDefaultParameterAsLocalVariable(GeneratorStringBuilder builder, string variableName,
        string defaultValue, UhtFunction function, UhtProperty paramProperty)
    {
        if (defaultValue == "()")
        {
            base.ExportCppDefaultParameterAsLocalVariable(builder, variableName, defaultValue, function, paramProperty);
            return;
        }
        if (!TryPath(paramProperty, defaultValue, out string expression))
            throw new InvalidOperationException("Unsupported soft path passed the default-value preflight.");
        builder.AppendLine($"{GetManagedType(paramProperty)} {variableName} = {expression};");
    }
}

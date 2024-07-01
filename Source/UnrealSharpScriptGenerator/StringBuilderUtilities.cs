using System.Collections.Generic;
using System.Text;
using EpicGames.UHT.Types;
using EpicGames.UHT.Utils;

namespace UnrealSharpScriptGenerator;

public static class StringBuilderUtilities
{
    public static void OpenBrace(this StringBuilder sb)
    {
        sb.AppendLine("{");
    }
    
    public static void CloseBrace(this StringBuilder sb)
    {
        sb.AppendLine("}");
    }
    
    public static void CloseBraceWithSemicolon(this StringBuilder sb)
    {
        sb.AppendLine("};");
    }
    
    public static void DeclareDirective(this StringBuilder sb, string directive)
    {
        sb.AppendLine($"using {directive};");
    }
    
    public static void Indent(this StringBuilder sb, int indentLevel = 1)
    {
        sb.Append(new string(' ', indentLevel * 4));
    }
    
    public static void UnIndent(this StringBuilder sb, int indentLevel = 1)
    {
        sb.Remove(sb.Length - indentLevel * 4, indentLevel * 4);
    }
    
    public static void GenerateTypeSkeleton(this StringBuilder sb, string typeNameSpace)
    {
        DeclareDirective(sb, ScriptGeneratorUtilities.EngineNamespace);
        DeclareDirective(sb, ScriptGeneratorUtilities.AttributeNamespace);
        DeclareDirective(sb, ScriptGeneratorUtilities.InteropNamespace);
        DeclareDirective(sb, "System.Runtime");
        DeclareDirective(sb, "System.Runtime.InteropServices");

        sb.AppendLine();
        sb.AppendLine($"namespace {typeNameSpace};");
        sb.AppendLine();
    }
    
    public static void DeclareType(this StringBuilder sb, string typeName, string declaredTypeName, string? baseType = null, bool isPartial = true, List<string>? interfaces = default)
    {
        string partialSpecifier = isPartial ? "partial" : string.Empty;
        string baseSpecifier = baseType != null ? $" : {baseType}" : string.Empty;
        string interfaceSpecifier = interfaces is { Count: > 0 } ? $" : {string.Join(", ", interfaces)}" : string.Empty;
        
        sb.AppendLine($"public {partialSpecifier}{typeName} {declaredTypeName}{baseSpecifier} {interfaceSpecifier}");
        sb.OpenBrace();
    }
}
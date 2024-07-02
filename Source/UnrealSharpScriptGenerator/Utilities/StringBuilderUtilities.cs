using System.Collections.Generic;
using System.Text;
using EpicGames.Core;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.Utilities;

public static class StringBuilderUtilities
{
    public static void OpenBrace(this StringBuilder sb)
    {
        sb.AppendLine("{");
        Indent(sb);
    }
    
    public static void CloseBrace(this StringBuilder sb)
    {
        sb.AppendLine("}");
        UnIndent(sb);
    }
    
    public static void CloseBraceWithSemicolon(this StringBuilder sb)
    {
        sb.AppendLine("};");
    }
    
    public static void DeclareDirective(this StringBuilder sb, string directive)
    {
        sb.AppendLine($"using {directive};");
    }
    
    public static void DeclareDirectives(this StringBuilder sb, List<string> directives)
    {
        List<string> addedDirectives = new();
        foreach (string directive in directives)
        {
            if (addedDirectives.Contains(directive))
            {
                continue;
            }
            
            DeclareDirective(sb, directive);
            addedDirectives.Add(directive);
        }
    }
    
    public static void Indent(this StringBuilder sb, int indentLevel = 1)
    {
        sb.Append(new string(' ', indentLevel * 4));
    }
    
    public static void UnIndent(this StringBuilder sb, int indentLevel = 1)
    {
        sb.Remove(sb.Length - indentLevel * 4, indentLevel * 4);
    }
    
    public static void BeginPreproccesorBlock(this StringBuilder sb, string condition)
    {
        sb.AppendLine($"#if {condition}");
    }
    
    public static void EndPreproccesorBlock(this StringBuilder sb)
    {
        sb.AppendLine("#endif");
    }
    
    public static void BeginWithEditorPreproccesorBlock(this StringBuilder sb)
    {
        BeginPreproccesorBlock(sb, "WITH_EDITOR");
    }
    
    public static void TryAddWithEditor(this StringBuilder sb, UhtProperty property)
    {
        if (property.HasAllFlags(EPropertyFlags.EditorOnly))
        {
            BeginWithEditorPreproccesorBlock(sb);
        }
    }
    
    public static void TryEndWithEditor(this StringBuilder sb, UhtProperty property)
    {
        if (property.HasAllFlags(EPropertyFlags.EditorOnly))
        {
            EndPreproccesorBlock(sb);
        }
    }
    
    public static void TryEndWithEditor(this StringBuilder sb, UhtFunction function)
    {
        if (function.FunctionFlags.HasAllFlags(EFunctionFlags.EditorOnly))
        {
            BeginWithEditorPreproccesorBlock(sb);
        }
    }
    
    public static void TryAddWithEditor(this StringBuilder sb, UhtFunction function)
    {
        if (function.FunctionFlags.HasAllFlags(EFunctionFlags.EditorOnly))
        {
            BeginWithEditorPreproccesorBlock(sb);
        }
    }
    
    public static void BeginUnsafeBlock(this StringBuilder sb)
    {
        sb.AppendLine("unsafe");
        sb.OpenBrace();
    }
    
    public static void EndUnsafeBlock(this StringBuilder sb)
    {
        sb.CloseBrace();
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
    
    public static void DeclareType(this StringBuilder sb, string typeName, string declaredTypeName, string? baseType = null, bool isPartial = true, List<UhtType>? interfaces = default)
    {
        string partialSpecifier = isPartial ? "partial" : string.Empty;
        string baseSpecifier = baseType != null ? $" : {baseType}" : string.Empty;
        string interfacesDeclaration = string.Empty;

        if (interfaces != null)
        {
            foreach (UhtType @interface in interfaces)
            {
                string cleanInterfaceName = ScriptGeneratorUtilities.GetCleanTypeName(@interface);
                interfacesDeclaration += $", I{cleanInterfaceName}";
            }
        }

        sb.AppendLine($"public {partialSpecifier}{typeName} {declaredTypeName}{baseSpecifier} {interfacesDeclaration}");
        sb.OpenBrace();
    }
}
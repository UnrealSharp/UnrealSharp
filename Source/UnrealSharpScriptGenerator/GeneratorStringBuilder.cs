using System;
using System.Collections.Generic;
using System.Text;
using EpicGames.Core;
using EpicGames.UHT.Types;
using EpicGames.UHT.Utils;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator;

public class GeneratorStringBuilder : IDisposable
{
    private int _indent;
    private readonly List<string> _directives = new();
    private BorrowStringBuilder _borrower = new(StringBuilderCache.Big);
    private StringBuilder StringBuilder => _borrower.StringBuilder;

    public override string ToString()
    {
        return StringBuilder.ToString();
    }

    public void Dispose()
    {
        _borrower.Dispose();
    }
    
    public void OpenBrace()
    {
        AppendLine("{");
        Indent();
    }
    
    public void CloseBrace()
    {
        UnIndent();
        AppendLine("}");
    }

    public void Indent()
    {
       ++_indent;
    }
    
    public void UnIndent()
    {
        --_indent;
    }
    
    public void Append(string text)
    {
        StringBuilder.Append(text);
    }

    public void AppendLine()
    {
        if (StringBuilder.Length > 0)
        {
            StringBuilder.AppendLine();
        }
        
        for (int i = 0; i < _indent; i++)
        {
            StringBuilder.Append("    ");
        }
    }
    
    public void AppendLine(string line)
    {
        AppendLine();
        StringBuilder.Append(line);
    }
    
    public void DeclareDirective(string directive)
    {
        if (_directives.Contains(directive) || string.IsNullOrEmpty(directive))
        {
            return;
        }
        
        _directives.Add(directive);
        AppendLine($"using {directive};");
    }
    
    public void DeclareDirectives(List<string> directives)
    {
        foreach (string directive in directives)
        {
            DeclareDirective(directive);
        }
    }
    
    public void BeginPreproccesorBlock(string condition)
    {
        AppendLine($"#if {condition}");
    }
    
    public void EndPreproccesorBlock()
    {
        AppendLine("#endif");
    }
    
    public void BeginWithEditorPreproccesorBlock()
    {
        BeginPreproccesorBlock("WITH_EDITOR");
    }
    
    public void TryAddWithEditor(UhtProperty property)
    {
        if (property.HasAllFlags(EPropertyFlags.EditorOnly))
        {
            BeginWithEditorPreproccesorBlock();
        }
    }
    
    public void TryEndWithEditor(UhtProperty property)
    {
        if (property.HasAllFlags(EPropertyFlags.EditorOnly))
        {
            EndPreproccesorBlock();
        }
    }
    
    public void TryEndWithEditor(UhtFunction function)
    {
        if (function.FunctionFlags.HasAllFlags(EFunctionFlags.EditorOnly))
        {
            EndPreproccesorBlock();
        }
    }
    
    public void TryAddWithEditor(UhtFunction function)
    {
        if (function.FunctionFlags.HasAllFlags(EFunctionFlags.EditorOnly))
        {
            BeginWithEditorPreproccesorBlock();
        }
    }
    
    public void BeginUnsafeBlock()
    {
        AppendLine("unsafe");
        OpenBrace();
    }
    
    public void EndUnsafeBlock()
    {
        CloseBrace();
    }
    
    public void GenerateTypeSkeleton(string typeNameSpace)
    {
        DeclareDirective(ScriptGeneratorUtilities.AttributeNamespace);
        DeclareDirective(ScriptGeneratorUtilities.InteropNamespace);

        AppendLine();
        AppendLine($"namespace {typeNameSpace};");
        AppendLine();
    }
    
    public void DeclareType(string typeName, string declaredTypeName, string? baseType = null, bool isPartial = true, List<UhtClass>? interfaces = default)
    {
        string partialSpecifier = isPartial ? "partial " : string.Empty;
        string baseSpecifier = !string.IsNullOrEmpty(baseType) ? $" : {baseType}" : string.Empty;
        string interfacesDeclaration = string.Empty;

        if (interfaces != null)
        {
            foreach (UhtType @interface in interfaces)
            {
                string fullInterfaceName = @interface.GetFullManagedName();
                interfacesDeclaration += $", {fullInterfaceName}";
            }
        }

        AppendLine($"public {partialSpecifier}{typeName} {declaredTypeName}{baseSpecifier}{interfacesDeclaration}");
        OpenBrace();
    }
}
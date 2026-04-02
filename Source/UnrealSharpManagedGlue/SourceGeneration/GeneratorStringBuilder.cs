using System;
using System.Collections.Generic;
using EpicGames.UHT.Utils;

namespace UnrealSharpManagedGlue.SourceGeneration;

public class GeneratorStringBuilder : IDisposable
{
    private int _indent;
    private readonly List<string> _directives = new();
    private readonly BorrowStringBuilder _borrower = new(StringBuilderCache.Big);
    private System.Text.StringBuilder StringBuilder => _borrower.StringBuilder;

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
    
    public void BeginPreproccesorBlock(string condition)
    {
        AppendLine($"#if {condition}");
    }
    
    public void EndPreproccesorBlock()
    {
        AppendLine("#endif");
    }
}
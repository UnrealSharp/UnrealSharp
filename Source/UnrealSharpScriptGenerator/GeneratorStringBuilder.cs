using System;
using System.Collections.Generic;
using System.Text;
using EpicGames.UHT.Utils;

namespace UnrealSharpScriptGenerator;

public class GeneratorStringBuilder : IDisposable
{
    private int _indent;
    private List<string> _directives = new();
    private readonly BorrowStringBuilder _borrower = new(StringBuilderCache.Big);
    private StringBuilder StringBuilder => _borrower.StringBuilder;

    public void Dispose()
    {
        _borrower.Dispose();
    }
    
    public void OpenBrace()
    {
        StringBuilder.AppendLine("{");
        Indent();
    }
    
    public void CloseBrace()
    {
        StringBuilder.AppendLine("}");
        UnIndent();
    }

    public void Indent()
    {
       ++_indent;
    }
    
    public void UnIndent()
    {
        --_indent;
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
}
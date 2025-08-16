using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace UnrealSharp.SourceGenerators;

public class SourceBuilder : IDisposable
{
    
    private readonly StringBuilder _stringBuilder;
    private readonly IndentedTextWriter _indentedTextWriter;

    public SourceBuilder()
    {
        _stringBuilder = new StringBuilder();
        _indentedTextWriter = new IndentedTextWriter(new StringWriter(_stringBuilder), "    ");
    }

    public int Indent
    {
        get => _indentedTextWriter.Indent;
        set => _indentedTextWriter.Indent = value;
    }

    public Scope OpenBlock()
    {
        return new Scope(this);
    }

    public SourceBuilder Append(string line)
    {
        _indentedTextWriter.Write(line);
        return this;
    }

    public SourceBuilder AppendLine(string line)
    {
        _indentedTextWriter.WriteLine(line);
        return this;
    }

    public SourceBuilder AppendLine()
    {
        return AppendLine(string.Empty);
    }

    public override string ToString()
    {
        return _stringBuilder.ToString();
    }

    public void Dispose()
    {
        _indentedTextWriter.Dispose();
    }
    
    public readonly struct Scope : IDisposable
    {
        private readonly SourceBuilder _sourceBuilder;

        internal Scope(SourceBuilder sourceBuilder)
        {
            _sourceBuilder = sourceBuilder;
            _sourceBuilder.AppendLine("{");
            _sourceBuilder.Indent++;
        }

        public void Dispose()
        {
            _sourceBuilder.Indent--;
            _sourceBuilder.AppendLine("}");
        }
    }
}
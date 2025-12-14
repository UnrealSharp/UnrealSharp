using System.Text;

namespace UnrealSharp.GlueGenerator;

public class GeneratorStringBuilder
{
    private readonly StringBuilder _stringBuilder = new(2048);

    private int _indent;
    private bool _needIndent;

    public override string ToString()
    {
        return _stringBuilder.ToString();
    }
    
    private void EnsureIndent()
    {
        if (!_needIndent)
        {
            return;
        }
        
        if (_indent > 0)
        {
            _stringBuilder.Append(' ', _indent * 4);
        }
        
        _needIndent = false;
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
        _indent++;
    }

    public void UnIndent()
    {
        _indent--;
    }
    
    public void Append(string text)
    {
        EnsureIndent();
        _stringBuilder.Append(text);
    }

    public void AppendLine()
    {
        if (_stringBuilder.Length > 0)
        {
            _stringBuilder.AppendLine();
        }
        
        _needIndent = true;
    }

    public void AppendLine(string line)
    {
        if (_stringBuilder.Length > 0)
        {
            _stringBuilder.AppendLine();
        }

        if (_indent > 0)
        {
            _stringBuilder.Append(' ', _indent * 4);
        }

        _stringBuilder.Append(line);
        _needIndent = false;
    }
}
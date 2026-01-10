using System.Collections.Generic;

namespace UnrealSharpManagedGlue.Tooltip;

public class ParsedTooltip
{
    public class TokenString
    {
        public string SimpleValue = string.Empty;
        public string ComplexValue = string.Empty;
        public string Value => !string.IsNullOrEmpty(SimpleValue) ? SimpleValue : ComplexValue;

        public bool Equals(TokenString other)
        {
            return Value == other.Value;
        }

        public bool NotEquals(TokenString other)
        {
            return Value != other.Value;
        }

        public void SetValue(string value)
        {
            SimpleValue = value;
            ComplexValue = string.Empty;
        }

        public void SetValue(ref string value)
        {
            SimpleValue = string.Empty;
            ComplexValue = value;
        }
    }

    public class MiscToken
    {
        public TokenString TokenName = new();
        public TokenString TokenValue = new();
    }

    public class ParamToken
    {
        public TokenString ParamName = new();
        public TokenString ParamType = new();
        public TokenString ParamComment = new();
    }
    
    public string BasicTooltipText = string.Empty;
    public readonly List<MiscToken> MiscTokens = new(4);
    public readonly List<ParamToken> ParamTokens= new(8);
    public readonly ParamToken ReturnToken = new();
}
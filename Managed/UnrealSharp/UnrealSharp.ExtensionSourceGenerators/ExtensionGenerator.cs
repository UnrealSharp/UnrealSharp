using System.Text;

namespace UnrealSharp.ExtensionSourceGenerators;

public abstract record ExtensionGenerator
{
    public abstract void Generate(StringBuilder builder, ParseResult parseResult);
}
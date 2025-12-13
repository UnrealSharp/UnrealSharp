using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record SingleDelegateProperty : DelegateProperty
{
    public SingleDelegateProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer, SyntaxNode? syntaxNode = null) 
        : base(memberSymbol, typeSymbol, PropertyType.Delegate, outer, "SingleDelegateMarshaller", syntaxNode)
    {
    }
    
    public override void ExportFromNative(GeneratorStringBuilder builder, string buffer, string? assignmentOperator = null)
    {
        AppendFromNative(builder);
    }
}
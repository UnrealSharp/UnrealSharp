using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record MulticastDelegateProperty : DelegateProperty
{
    public MulticastDelegateProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer, SyntaxNode? syntaxNode = null) 
        : base(memberSymbol, typeSymbol, PropertyType.MulticastInlineDelegate, outer, "MulticastDelegateMarshaller", syntaxNode)
    {
        
    }

    public MulticastDelegateProperty(EquatableArray<UnrealProperty> templateParameters, string sourceName, Accessibility accessibility, UnrealType outer) : base(templateParameters, new FieldName("TMulticastDelegate"), PropertyType.MulticastInlineDelegate, "MulticastDelegateMarshaller", sourceName, accessibility, outer)
    {
    }

    public override void ExportFromNative(GeneratorStringBuilder builder, string buffer, string? assignmentOperator = null)
    {
        AppendFromNative(builder, NativePropertyVariable);
    }
}
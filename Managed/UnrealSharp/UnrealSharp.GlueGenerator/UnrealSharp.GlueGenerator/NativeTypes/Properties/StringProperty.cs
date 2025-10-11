using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record StringProperty : SimpleProperty
{
    public override string MarshallerType => "StringMarshaller";

    public StringProperty(SyntaxNode syntaxNode, ISymbol memberSymbol, ITypeSymbol? typeSymbol, UnrealType outer) 
        : base(syntaxNode, memberSymbol, typeSymbol, PropertyType.String, outer)
    {
        
    }
    
    public StringProperty(string sourceName, Accessibility accessibility, UnrealType outer) 
        : base(PropertyType.String, "System.String", sourceName, accessibility, outer)
    {
    }
}
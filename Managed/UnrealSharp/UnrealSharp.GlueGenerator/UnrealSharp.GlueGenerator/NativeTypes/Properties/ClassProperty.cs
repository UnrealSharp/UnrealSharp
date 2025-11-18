using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record ClassProperty : TemplateProperty
{
    public ClassProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer) 
        : base(memberSymbol, typeSymbol, PropertyType.Class, outer, "SubclassOfMarshaller")
    {

    }
}
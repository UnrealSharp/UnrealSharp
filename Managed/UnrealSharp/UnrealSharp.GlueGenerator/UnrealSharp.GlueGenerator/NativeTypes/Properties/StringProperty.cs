using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record StringProperty : SimpleProperty
{
    public override string MarshallerType => "StringMarshaller";
    
    static readonly FieldName DefaultFieldName = new FieldName("string");

    public StringProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer) 
        : base(memberSymbol, typeSymbol, PropertyType.String, outer)
    {
        
    }
    
    public StringProperty(string sourceName, Accessibility accessibility, UnrealType outer) 
        : base(PropertyType.String, DefaultFieldName, sourceName, accessibility, outer)
    {
    }
}
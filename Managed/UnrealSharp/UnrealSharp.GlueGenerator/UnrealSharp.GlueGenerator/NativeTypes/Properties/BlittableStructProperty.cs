using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record BlittableStructProperty : BlittableProperty
{
    public BlittableStructProperty(SyntaxNode syntaxNode, ISymbol memberSymbol, ITypeSymbol? typeSymbol, PropertyType propertyType, UnrealType outer) 
        : base(syntaxNode, memberSymbol, typeSymbol, propertyType, outer)
    {
        CacheNativeTypePtr = true;
    }

    public override void MakeProperty(GeneratorStringBuilder builder, string ownerPtr)
    {
        base.MakeProperty(builder, ownerPtr);
        builder.AppendLine($"ModifyFieldProperty({BuilderNativePtr}, $\"{ShortEngineName}\", typeof({ManagedType}));");
    }
}
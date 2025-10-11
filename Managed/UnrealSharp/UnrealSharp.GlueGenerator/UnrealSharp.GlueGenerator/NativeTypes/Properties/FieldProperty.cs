using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record FieldProperty : SimpleProperty
{
    public FieldProperty(SyntaxNode syntaxNode, ISymbol memberSymbol, ITypeSymbol? typeSymbol, PropertyType propertyType, UnrealType outer) 
        : base(syntaxNode, memberSymbol, typeSymbol, propertyType, outer)
    {
        CacheNativeTypePtr = true;
    }

    public FieldProperty(PropertyType type, string managedType, string sourceName, Accessibility accessibility, UnrealType outer) 
        : base(type, managedType, sourceName, accessibility, outer)
    {
        CacheNativeTypePtr = true;
    }

    public override void MakeProperty(GeneratorStringBuilder builder, string ownerPtr)
    {
        base.MakeProperty(builder, ownerPtr);
        ExportFieldInfo(builder);
    }

    protected virtual void ExportFieldInfo(GeneratorStringBuilder builder)
    {
        builder.AppendLine($"ModifyFieldProperty({BuilderNativePtr}, \"{ShortEngineName}\", typeof({ManagedType}));");
    }
}
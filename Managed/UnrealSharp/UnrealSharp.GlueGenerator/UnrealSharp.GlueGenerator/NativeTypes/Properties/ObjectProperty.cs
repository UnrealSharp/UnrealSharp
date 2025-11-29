using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record ObjectProperty : FieldProperty
{
    public override string MarshallerType => DefaultComponent
        ? $"DefaultComponentMarshaller<{ManagedType}>"
        : $"ObjectMarshaller<{ManagedType}>";

    public ObjectProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer, SyntaxNode? syntaxNode = null) 
        : base(memberSymbol, typeSymbol, PropertyType.Object, outer, syntaxNode)
    {

    }
    
    public ObjectProperty(FieldName managedType, string sourceName, Accessibility accessibility, UnrealType outer) 
        : base(PropertyType.Object, managedType, managedType, sourceName, accessibility, outer)
    {

    }

    public override void ExportFromNative(GeneratorStringBuilder builder, string buffer, string? assignmentOperator = null)
    {
        if (DefaultComponent)
        {
            builder.Append($"{MarshallerType}{FromNative}(this, \"{SourceName}\", {AppendOffsetMath(SourceGenUtilities.NativeObject)}, 0);");
        }
        else
        {
            base.ExportFromNative(builder, buffer, assignmentOperator);
        }
    }
}
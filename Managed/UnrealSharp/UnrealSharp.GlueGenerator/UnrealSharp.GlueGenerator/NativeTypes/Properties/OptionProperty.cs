using System.Linq;
using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record OptionProperty : ContainerProperty
{
    const string OptionMarshaller = "OptionMarshaller";
    
    public OptionProperty(SyntaxNode syntaxNode, ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer) 
        : base(syntaxNode, memberSymbol, typeSymbol, PropertyType.Optional, outer)
    {

    }

    protected override void ExportSetter(GeneratorStringBuilder builder)
    {
        builder.AppendLine("set");
        builder.OpenBrace();
        ExportToNative(builder, SourceGenUtilities.NativeObject, SourceGenUtilities.ValueParam);
        builder.CloseBrace();
    }
    
    public override void ExportToNative(GeneratorStringBuilder builder, string buffer, string value)
    {
        string delegates = string.Join(", ", InnerTypes.Select(t => t).Select(t => $"{t.CallToNative}, {t.CallFromNative}"));
        builder.AppendLine($"{InstancedMarshallerVariable} ??= new {MarshallerType}({NativePropertyVariable}, {delegates});");
        builder.AppendLine();
        
        AppendCallToNative(builder, InstancedMarshallerVariable, buffer, value);
    }

    protected override string GetFieldMarshaller() => OptionMarshaller;
    protected override string GetCopyMarshaller() => OptionMarshaller;
}
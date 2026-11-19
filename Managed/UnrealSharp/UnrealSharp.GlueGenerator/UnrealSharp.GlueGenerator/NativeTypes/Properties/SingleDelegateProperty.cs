using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record SingleDelegateProperty : DelegateProperty
{
    public SingleDelegateProperty(ISymbol symbol, ITypeSymbol typeSymbol, UnrealType outer,
        SyntaxNode? syntaxNode = null)
        : base(symbol, typeSymbol, PropertyType.Delegate, outer, "SingleDelegateMarshaller", syntaxNode)
    {
    }

    public override void ExportFromNative(GeneratorStringBuilder builder, string buffer,
        string? assignmentOperator = null)
    {
        if (Outer is UnrealClass)
        {
            base.ExportFromNative(builder, buffer, assignmentOperator);
            return;
        }

        builder.AppendLine($"{assignmentOperator}{CallFromNative}({AppendOffsetMath(buffer)}, 0, IntPtr.Zero);");
    }

    protected override void ExportSetter(GeneratorStringBuilder builder)
    {
        builder.OpenBrace();
        builder.AppendLine($"if (value == {BackingFieldName})");
        builder.OpenBrace();
        builder.AppendLine("return;");
        builder.CloseBrace();

        ExportToNative(builder, SourceGenUtilities.NativeObject, SourceGenUtilities.ValueParam);
        builder.AppendLine($"{BackingFieldName} = null;");
        builder.CloseBrace();
    }
}

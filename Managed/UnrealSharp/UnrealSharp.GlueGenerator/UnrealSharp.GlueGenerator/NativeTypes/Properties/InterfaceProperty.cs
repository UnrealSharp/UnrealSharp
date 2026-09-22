using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record InterfaceProperty : FieldProperty
{
    public override string MarshallerType => ManagedType + "Marshaller";

    // Preserve the user's non-null contract at the native boundary, as ObjectMarshaller does.
    public override string CallFromNative => IsNullable
        ? base.CallFromNative
        : $"(__nativeBuffer, __arrayIndex) => {base.CallFromNative}(__nativeBuffer, __arrayIndex)!";

    public override string NullValue => IsNullable ? "null" : "null!";

    public InterfaceProperty(ISymbol symbol, ITypeSymbol typeSymbol, UnrealType outer,
        SyntaxNode? syntaxNode = null)
        : base(symbol, typeSymbol, PropertyType.ScriptInterface, outer, syntaxNode)
    {
    }

    public override void ExportFromNative(GeneratorStringBuilder builder, string buffer,
        string? assignmentOperator = null)
    {
        string nullableSuppression = IsNullable ? string.Empty : "!";
        builder.Append($"{assignmentOperator}{MarshallerType}.FromNative({AppendOffsetMath(buffer)}, 0){nullableSuppression};");
    }
}
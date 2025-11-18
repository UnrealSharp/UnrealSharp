using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record SingleDelegateProperty : DelegateProperty
{
    public SingleDelegateProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, UnrealType outer) 
        : base(memberSymbol, typeSymbol, PropertyType.Delegate, outer, "SingleDelegateMarshaller")
    {
    }
    
    public override void ExportFromNative(GeneratorStringBuilder builder, string buffer, string? assignmentOperator = null)
    {
        AppendFromNative(builder);
    }
}
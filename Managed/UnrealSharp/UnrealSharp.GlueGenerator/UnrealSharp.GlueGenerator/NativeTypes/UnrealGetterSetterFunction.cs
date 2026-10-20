using Microsoft.CodeAnalysis;
using UnrealSharp.GlueGenerator.NativeTypes.Properties;

namespace UnrealSharp.GlueGenerator.NativeTypes;

public record UnrealGetterSetterFunction : UnrealFunction
{
    private readonly string _propertyName;

    public UnrealGetterSetterFunction(UnrealProperty property, IMethodSymbol typeSymbol, UnrealType outer) : base(
        typeSymbol, outer)
    {
        _propertyName = property.FieldName.SourceName;
        string newSourceName =
            HasReturnValue ? $"Get{property.FieldName.SourceName}" : $"Set{property.FieldName.SourceName}";
        FieldName = FieldName.InScopeOf(FieldName, newSourceName, FieldType.Unknown);
    }

    protected override void ExportInvokeMethodCallSignature(GeneratorStringBuilder builder)
    {
        if (HasReturnValue)
        {
            builder.AppendLine($"{ReturnType.ManagedTypeWithNullability} returnValue = {_propertyName};");
        }
        else
        {
            builder.AppendLine($"{_propertyName} = value;");
        }
    }
}
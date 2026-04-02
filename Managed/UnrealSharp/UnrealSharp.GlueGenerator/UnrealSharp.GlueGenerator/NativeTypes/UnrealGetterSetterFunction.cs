using Microsoft.CodeAnalysis;
using UnrealSharp.GlueGenerator.NativeTypes.Properties;

namespace UnrealSharp.GlueGenerator.NativeTypes;

public record UnrealGetterSetterFunction : UnrealFunction
{
    private readonly string _propertyName;
    
    public UnrealGetterSetterFunction(UnrealProperty property, IMethodSymbol typeSymbol, UnrealType outer) : base(typeSymbol, outer)
    {
        _propertyName = property.SourceName;
        SourceName = HasReturnValue ? $"Get{property.SourceName}" : $"Set{property.SourceName}";
    }

    protected override void ExportInvokeMethodCallSignature(GeneratorStringBuilder builder)
    {
        if (HasReturnValue)
        {
            builder.AppendLine($"{ReturnType.ManagedType} returnValue = {_propertyName};");
        }
        else
        {
            builder.AppendLine($"{_propertyName} = value;");
        }
    }
}
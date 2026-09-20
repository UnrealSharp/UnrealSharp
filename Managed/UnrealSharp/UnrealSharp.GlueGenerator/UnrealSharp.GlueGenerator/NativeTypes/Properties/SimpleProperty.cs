using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record SimpleProperty : UnrealProperty
{
    public SimpleProperty(ISymbol symbol, ITypeSymbol typeSymbol, PropertyType propertyType, UnrealType outer,
        SyntaxNode? syntaxNode = null)
        : base(symbol, typeSymbol, propertyType, outer, syntaxNode)
    {
        ManagedType = ManagedTypeName.FromSymbol(typeSymbol);
    }

    public SimpleProperty(PropertyType type, ManagedTypeName managedType, string sourceName,
        Accessibility accessibility, UnrealType outer)
        : base(type, sourceName, accessibility, outer)
    {
        ManagedType = managedType;
    }

    protected override void ExportSetter(GeneratorStringBuilder builder)
    {
        if (FieldNotify)
        {
            builder.OpenBrace();
            builder.AppendLine();
            ExportToNative(builder, SourceGenUtilities.NativeObject, SourceGenUtilities.ValueParam);
            builder.AppendLine(
                $"UnrealSharp.Engine.UFieldNotificationLibrary.BroadcastFieldValueChanged(this, new UnrealSharp.FieldNotification.FFieldNotificationId(nameof({FieldName.SourceName})));");
            builder.CloseBrace();
        }
        else
        {
            base.ExportSetter(builder);
        }
    }
}
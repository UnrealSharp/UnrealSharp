using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record SimpleProperty : UnrealProperty
{
    public SimpleProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, PropertyType propertyType, UnrealType outer, SyntaxNode? syntaxNode = null) 
        : base(memberSymbol, typeSymbol, propertyType, outer, syntaxNode)
    {
        ManagedType = new FieldName(typeSymbol);
    }

    public SimpleProperty(PropertyType type, FieldName managedType, string sourceName, Accessibility accessibility, UnrealType outer) : base(type, sourceName, accessibility, outer)
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
            builder.AppendLine($"UnrealSharp.Engine.UFieldNotificationLibrary.BroadcastFieldValueChanged(this, new UnrealSharp.FieldNotification.FFieldNotificationId(nameof({SourceName})));");
            builder.CloseBrace();
        }
        else
        {
            base.ExportSetter(builder);
        }
    }
}
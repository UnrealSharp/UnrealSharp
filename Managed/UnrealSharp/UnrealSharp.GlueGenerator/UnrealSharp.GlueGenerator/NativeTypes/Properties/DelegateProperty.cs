using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record DelegateProperty : TemplateProperty
{
    public DelegateProperty(EquatableArray<UnrealProperty> templateParameters, FieldName fieldName, PropertyType propertyType, string marshaller, string sourceName, Accessibility accessibility, UnrealType outer) : base(templateParameters, fieldName, propertyType, marshaller, sourceName, accessibility, outer)
    {
    }
    
    public DelegateProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, PropertyType propertyType, UnrealType outer, string marshaller, SyntaxNode? syntaxNode = null) 
        : base(memberSymbol, typeSymbol, propertyType, outer, marshaller, syntaxNode)
    {
        NeedsBackingFields = false;
    }

    private string BackingFieldName => $"{SourceName}_BackingField";
    
    protected void AppendFromNative(GeneratorStringBuilder builder, string? nativeProperty = null)
    {
        string nativePropertyParam = nativeProperty != null ? $", {nativeProperty}" : string.Empty;
        builder.AppendLine($"{BackingFieldName} ??= {CallFromNative}({AppendOffsetMath(SourceGenUtilities.NativeObject)}{nativePropertyParam}, 0);");
        builder.AppendLine($"return {BackingFieldName};");
    }

    public override void ExportBackingVariables(GeneratorStringBuilder builder, string nativePtrType)
    {
        base.ExportBackingVariables(builder, nativePtrType);
        ExportNativeProperty(builder, nativePtrType);
    }

    public override void ExportType(GeneratorStringBuilder builder, SourceProductionContext spc)
    {
        builder.AppendEditorBrowsableAttribute();
        builder.AppendLine($"{ManagedType}? {BackingFieldName} = null;");
        builder.AppendLine();
        
        base.ExportType(builder, spc);
    }

    protected override void ExportGetter(GeneratorStringBuilder builder)
    {
        builder.OpenBrace();
        ExportFromNative(builder, SourceGenUtilities.NativeObject, SourceGenUtilities.ReturnAssignment);
        builder.CloseBrace();
    }

    public override void ExportToNative(GeneratorStringBuilder builder, string buffer, string value)
    {
        builder.AppendLine($"if (value == {BackingFieldName})");
        builder.AppendLine("{");
        builder.AppendLine("   return;");
        builder.AppendLine("}");
        builder.AppendLine($"{BackingFieldName} = value;");
        builder.AppendLine($"{CallToNative}({AppendOffsetMath(SourceGenUtilities.NativeObject)}, 0, value);");
    }

    protected override void ExportSetter(GeneratorStringBuilder builder)
    {
        builder.OpenBrace();
        ExportToNative(builder, SourceGenUtilities.NativeObject, SourceGenUtilities.ValueParam);
        builder.CloseBrace();
    }
    
    public static string MakeDelegateSignatureName(string fullDelegateName)
    {
        return fullDelegateName + "__DelegateSignature";
    }
    
    public static FieldName MakeFieldNameFromDelegateSymbol(ITypeSymbol typeSymbol)
    {
        string unrealDelegateName = MakeDelegateSignatureName(typeSymbol.Name);
        return new FieldName(unrealDelegateName, typeSymbol.ContainingNamespace.ToDisplayString(), typeSymbol.ContainingAssembly.Name);
    }
}
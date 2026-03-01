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
        
    }
    
    public override bool NeedsBackingNativeProperty => true;
    
    protected string BackingFieldName => $"{SourceName}_BackingField";

    public override void ExportBackingVariables(GeneratorStringBuilder builder)
    {
        base.ExportBackingVariables(builder);
        builder.AppendNewBackingField($"{ManagedType}? {BackingFieldName} = null;");
    }

    protected override void ExportGetter(GeneratorStringBuilder builder)
    {
        builder.OpenBrace();
        ExportFromNative(builder, SourceGenUtilities.NativeObject, SourceGenUtilities.ReturnAssignment);
        builder.CloseBrace();
    }

    public override void ExportFromNative(GeneratorStringBuilder builder, string buffer, string? assignmentOperator = null)
    {
        builder.AppendLine($"{BackingFieldName} ??= {CallFromNative}({AppendOffsetMath(SourceGenUtilities.NativeObject)}, 0, {NativePropertyVariable});");
        
        if (assignmentOperator != null)
        {
            builder.AppendLine($"{assignmentOperator}{BackingFieldName};");
        }
    }

    public override void ExportToNative(GeneratorStringBuilder builder, string buffer, string value)
    {
        builder.AppendLine($"{CallToNative}({AppendOffsetMath(SourceGenUtilities.NativeObject)}, 0, {value});");
    }

    protected override void ExportSetter(GeneratorStringBuilder builder)
    {
        builder.OpenBrace();
        
        builder.AppendLine($"if (value == {BackingFieldName})");
        builder.OpenBrace();
        builder.AppendLine("return;");
        builder.CloseBrace();
        
        builder.AppendLine($"{BackingFieldName} = value;");
        
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
        return new FieldName(unrealDelegateName, typeSymbol.GetNamespace(), typeSymbol.ContainingAssembly.Name);
    }
}
using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record DelegateProperty : FieldProperty
{
    public override string MarshallerType => _isMulticast ? 
        $"MulticastDelegateMarshaller<{_fullDelegateName}>" 
        : $"SingleDelegateMarshaller<{_fullDelegateName}>";
    
    private string BackingFieldName => $"{SourceName}_BackingField";
    
    private readonly bool _isMulticast;
    private readonly string _fullDelegateName;

    public DelegateProperty(SyntaxNode syntaxNode, ISymbol memberSymbol, ITypeSymbol typeSymbol, PropertyType propertyType, UnrealType outer, bool isMulticast) 
        : base(syntaxNode, memberSymbol, typeSymbol, propertyType, outer)
    {
        _isMulticast = isMulticast;
        NeedsBackingFields = false;
        
        INamedTypeSymbol namedTypeSymbol = (INamedTypeSymbol)typeSymbol;
        ITypeSymbol argumentTypeSymbol = namedTypeSymbol.TypeArguments[0];
        
        _fullDelegateName = argumentTypeSymbol.ToDisplayString();
        ManagedType = MakeDelegateType(_fullDelegateName);
        ShortEngineName = MakeDelegateSignatureName(argumentTypeSymbol.Name);
    }
    
    public DelegateProperty(PropertyType type, string delegateName, string sourceName, Accessibility accessibility, UnrealType outer) : base(type, delegateName, sourceName, accessibility, outer)
    {
        _isMulticast = type is PropertyType.MulticastInlineDelegate or PropertyType.MulticastSparseDelegate;
        _fullDelegateName = delegateName;
        ManagedType = MakeDelegateType(_fullDelegateName);
        ShortEngineName = MakeDelegateSignatureName(delegateName);
    }
    
    string MakeDelegateType(string fullDelegateName)
    {
        return _isMulticast ? $"TMulticastDelegate<{fullDelegateName}>" : $"TDelegate<{fullDelegateName}>";
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
        builder.AppendLine("get");
        builder.OpenBrace();
        ExportFromNative(builder, SourceGenUtilities.NativeObject, SourceGenUtilities.ReturnAssignment);
        builder.CloseBrace();
    }

    public override void ExportFromNative(GeneratorStringBuilder builder, string buffer, string? assignmentOperator = null)
    {
        string nativePropertyParam = _isMulticast ? $", {NativePropertyVariable}" : "";
        builder.AppendLine($"{BackingFieldName} ??= {CallFromNative}({AppendOffsetMath(SourceGenUtilities.NativeObject)}{nativePropertyParam}, 0);");
        builder.AppendLine($"return {BackingFieldName};");
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
        builder.AppendLine("set");
        builder.OpenBrace();
        ExportToNative(builder, SourceGenUtilities.NativeObject, SourceGenUtilities.ValueParam);
        builder.CloseBrace();
    }

    protected override void ExportFieldInfo(GeneratorStringBuilder builder)
    {
        builder.AppendLine($"ModifyFieldProperty({BuilderNativePtr}, \"{ShortEngineName}\", typeof({_fullDelegateName}));");
    }
    
    public static string MakeDelegateSignatureName(string fullDelegateName)
    {
        return fullDelegateName + "__DelegateSignature";
    }
}
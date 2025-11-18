using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record ContainerProperty : TemplateProperty
{
    private Func<string> ContainerMarshaller => Outer is UnrealClass ? GetFieldMarshaller : GetCopyMarshaller;
    public override string MarshallerType => MakeMarshallerType(ContainerMarshaller(), TemplateParameters.Select(t => t.ManagedType.FullName).ToArray());

    public ContainerProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, PropertyType propertyType, UnrealType outer)
        : base(memberSymbol, typeSymbol, propertyType, outer, "")
    {
        NeedsBackingFields = true;
        CanInstanceMarshallerBeStatic = outer is not UnrealClass;
    }

    protected override void ExportGetter(GeneratorStringBuilder builder)
    {
        builder.OpenBrace();
        ExportFromNative(builder, SourceGenUtilities.NativeObject, SourceGenUtilities.ReturnAssignment);
        builder.CloseBrace();
    }

    public override void ExportFromNative(GeneratorStringBuilder builder, string buffer, string? assignmentOperator = null)
    {
        string delegates = string.Join(", ", TemplateParameters.Select(t => t).Select(t => $"{t.CallToNative}, {t.CallFromNative}"));
        builder.AppendLine($"{InstancedMarshallerVariable} ??= new {MarshallerType}({NativePropertyVariable}, {delegates});");
        builder.AppendLine();
        AppendCallFromNative(builder, InstancedMarshallerVariable, buffer, assignmentOperator);
    }

    public override void ExportToNative(GeneratorStringBuilder builder, string buffer, string value)
    {
        string delegates = string.Join(", ", TemplateParameters.Select(t => t).Select(t => $"{t.CallToNative}, {t.CallFromNative}"));
        builder.AppendLine($"{InstancedMarshallerVariable} ??= new {MarshallerType}({NativePropertyVariable}, {delegates});");
        builder.AppendLine();
        AppendCallToNative(builder, InstancedMarshallerVariable, buffer, value);
    }

    protected override void ExportSetter(GeneratorStringBuilder builder)
    {
        builder.Append(" => throw new NotSupportedException();");
    }

    protected virtual string GetFieldMarshaller() => throw new NotImplementedException();
    protected virtual string GetCopyMarshaller() => throw new NotImplementedException();

    public virtual bool Equals(ContainerProperty? other)
    {
       return base.Equals(other);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
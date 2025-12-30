using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record ContainerProperty : TemplateProperty
{
    private Func<string> ContainerMarshaller => Outer is UnrealClass ? GetFieldMarshaller : GetCopyMarshaller;
    
    public override string MarshallerType => MakeMarshallerType(ContainerMarshaller(), TemplateParameters.Select(t => t.ManagedType.FullName).ToArray());
    public override bool NeedsCachedMarshaller => true;

    protected bool NeedsMarshallingDelegates = true;

    public ContainerProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, PropertyType propertyType, UnrealType outer, SyntaxNode? syntaxNode = null)
        : base(memberSymbol, typeSymbol, propertyType, outer, "", syntaxNode)
    {
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
        ExportMarshaller(builder);
        AppendCallFromNative(builder, InstancedMarshallerVariable, buffer, assignmentOperator);
    }

    public override void ExportToNative(GeneratorStringBuilder builder, string buffer, string value)
    {
        ExportMarshaller(builder);
        AppendCallToNative(builder, InstancedMarshallerVariable, buffer, value);
    }
    
    private void ExportMarshaller(GeneratorStringBuilder builder)
    {
        builder.AppendLine($"{InstancedMarshallerVariable} ??= new {MarshallerType}({NativePropertyVariable}");

        if (NeedsMarshallingDelegates)
        {
            builder.Append(", ");
            builder.Append(string.Join(", ", TemplateParameters.Select(t => $"{t.CallToNative}, {t.CallFromNative}")));
        }

        builder.Append(");");
        builder.AppendLine();
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
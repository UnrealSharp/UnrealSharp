using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using UnrealSharp.GlueGenerator.NativeTypes.Properties;

namespace UnrealSharp.GlueGenerator.NativeTypes;

public enum DelegateType : byte
{
    MulticastDelegate,
    SingleDelegate,
}

[Inspector]
public record UnrealDelegate : UnrealType
{
    public override FieldType FieldType => FieldType.Delegate;
    private readonly UnrealFunctionBase _delegateSignature;
    private readonly DelegateType _delegateType;
    private readonly string _managedDelegateName;

    public UnrealDelegate(DelegateType delegateType, ISymbol typeSymbol) : base(typeSymbol)
    {
        INamedTypeSymbol namedTypeSymbol = (INamedTypeSymbol)typeSymbol;

        _delegateType = delegateType;
        _managedDelegateName = namedTypeSymbol.Name;
        _delegateSignature = new UnrealFunction(namedTypeSymbol.DelegateInvokeMethod!, this);

        FieldName = new FieldName(DelegateProperty.MakeDelegateSignatureName(FieldName.SourceName),
            FieldName.EngineName, FieldName.Namespace, FieldName.AssemblyName, FieldType.Delegate);
        _delegateSignature.FieldName = FieldName;

        ApplyFunctionFlags(delegateType);
    }

    public UnrealDelegate(UnrealFunctionBase delegateSignature, DelegateType delegateType)
        : base(delegateSignature.FieldName.SourceName, delegateSignature.FieldName.Namespace,
            Accessibility.Public,
            delegateSignature.FieldName.AssemblyName,
            delegateSignature.Outer)
    {
        _delegateType = delegateType;
        _delegateSignature = delegateSignature;
        _managedDelegateName = FieldName.SourceName;

        FieldName = new FieldName(DelegateProperty.MakeDelegateSignatureName(_managedDelegateName),
            FieldName.Namespace, FieldName.AssemblyName, FieldType.Delegate);

        _delegateSignature.FieldName = FieldName;
        ApplyFunctionFlags(delegateType);
    }

    void ApplyFunctionFlags(DelegateType delegateType)
    {
        _delegateSignature.FunctionFlags |= EFunctionFlags.Delegate;

        if (delegateType == DelegateType.MulticastDelegate)
        {
            _delegateSignature.FunctionFlags |= EFunctionFlags.MulticastDelegate;
        }
    }

    [Inspect("UnrealSharp.Attributes.UMultiDelegateAttribute", "UMultiDelegateAttribute", "Global")]
    public static UnrealType UMultiDelegateAttribute(UnrealType? outer, SyntaxNode? syntaxNode,
        GeneratorAttributeSyntaxContext ctx, ISymbol symbol, IReadOnlyList<AttributeData> attributes)
    {
        return new UnrealDelegate(DelegateType.MulticastDelegate, symbol);
    }

    [Inspect("UnrealSharp.Attributes.USingleDelegateAttribute", "USingleDelegateAttribute", "Global")]
    public static UnrealType USingleDelegateAttribute(UnrealType? outer, SyntaxNode? syntaxNode,
        GeneratorAttributeSyntaxContext ctx, ISymbol symbol, IReadOnlyList<AttributeData> attributes)
    {
        return new UnrealDelegate(DelegateType.SingleDelegate, symbol);
    }

    public override void ExportType(GeneratorStringBuilder builder, SourceProductionContext spc)
    {
        builder.GenerateTypeRegistration(this);

        string baseTypeName = _delegateType == DelegateType.MulticastDelegate ? "MulticastDelegate" : "Delegate";
        string delegateWrapperClassName =
            DelegateProperty.MakeDelegateSignatureName(_delegateSignature.FieldName.SourceName);

        bool hasParameters = _delegateSignature.Properties.Count > 0;
        string args = string.Empty;
        string parameters = string.Empty;

        if (hasParameters)
        {
            args = string.Join(", ", _delegateSignature.Properties.Select(x => x.GetParameterDeclaration()));
            parameters = string.Join(", ", _delegateSignature.Properties.Select(x => x.GetParameterCall()));
        }

        TypeDeclarationBuilder typeDeclarationBuilder = TypeDeclarationBuilder
            .FromUnrealType(this, SourceGenUtilities.ClassKeyword)
            .WithNativePtr(_delegateSignature.FunctionNativePtr)
            .WithSourceName(delegateWrapperClassName)
            .WithDeclarationName(delegateWrapperClassName)
            .Extends($"{baseTypeName}<{_managedDelegateName}>");

        typeDeclarationBuilder.Build(builder);

        builder.BeginTypeStaticConstructor(delegateWrapperClassName);
        _delegateSignature.ExportBackingVariablesToStaticConstructor(builder, _delegateSignature.FunctionNativePtr);
        builder.EndTypeStaticConstructor();

        AppendInvoker(builder, args);

        builder.CloseBrace();
        builder.AppendLine();

        AppendExtensionsClass(builder, args, parameters);
    }

    void AppendInvoker(GeneratorStringBuilder builder, string args)
    {
        if (_delegateSignature.HasAnyProperties)
        {
            _delegateSignature.ExportBackingVariables(builder);
        }

        builder.AppendLine($"protected override {_managedDelegateName} GetInvoker() => Invoker;");

        builder.AppendLine($"private void Invoker({args})");
        builder.OpenBrace();
        _delegateSignature.ExportCallToNative(builder,
            (paramsbuffer, returnBuffer) => { builder.AppendLine($"ProcessDelegate({paramsbuffer});"); });
        builder.CloseBrace();
    }

    void AppendExtensionsClass(GeneratorStringBuilder builder, string args, string parameters)
    {
        string extensionsClassName = $"{_delegateSignature.FieldName.SourceName}Extensions";
        builder.AppendLine($"public static class {extensionsClassName}");
        builder.OpenBrace();
        builder.AppendLine(
            $"public static void Invoke(this TMulticastDelegate<{_managedDelegateName}> del{(args.Length > 0 ? ", " : string.Empty)}{args})");
        builder.Append($" => del.InnerDelegate.Invoke({parameters});");
        builder.CloseBrace();
    }

    public void AppendFunctionAsDelegate(GeneratorStringBuilder builder)
    {
        builder.AppendLine(
            $"public delegate {_delegateSignature.ReturnType.ManagedType} {_managedDelegateName}({string.Join(", ", _delegateSignature.Properties.Select(x => x.GetParameterDeclaration()))});");
    }

    public override void PopulateJsonObject(JsonWriter jsonWriter)
    {
        _delegateSignature.PopulateJsonObject(jsonWriter);
    }
}
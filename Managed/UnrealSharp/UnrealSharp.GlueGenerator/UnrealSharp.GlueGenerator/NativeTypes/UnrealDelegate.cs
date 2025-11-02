using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnrealSharp.GlueGenerator.NativeTypes.Properties;

namespace UnrealSharp.GlueGenerator.NativeTypes;

[Inspector]
public record UnrealDelegate : UnrealType
{
    private readonly UnrealFunctionBase _delegateSignature;
    private readonly bool _isMulticast;
    public override int FieldTypeValue => 4;

    public UnrealDelegate(bool isMulticast, SemanticModel model, ISymbol typeSymbol, SyntaxNode syntax) : base(typeSymbol, syntax)
    {
        _isMulticast = isMulticast;
        _delegateSignature = new UnrealFunction(model, typeSymbol, (DelegateDeclarationSyntax) syntax, this);
        SourceName = DelegateProperty.MakeDelegateSignatureName(SourceName);
        ApplyFunctionFlags(isMulticast);
    }

    public UnrealDelegate(UnrealFunctionBase delegateSignature, bool isMulticast) : base(delegateSignature.SourceName, delegateSignature.Namespace, Accessibility.Public, delegateSignature.AssemblyName, delegateSignature.Outer)
    {
        _isMulticast = isMulticast;
        _delegateSignature = delegateSignature;
        ApplyFunctionFlags(isMulticast);
    }
    
    void ApplyFunctionFlags(bool isMulticast)
    {
        _delegateSignature.FunctionFlags |= isMulticast ? EFunctionFlags.Delegate | EFunctionFlags.MulticastDelegate : EFunctionFlags.Delegate;
    }
    
    [Inspect("UnrealSharp.Attributes.UMultiDelegateAttribute", "UMultiDelegateAttribute", "Global")]
    public static UnrealType? UMultiDelegateAttribute(UnrealType? outer, GeneratorAttributeSyntaxContext ctx, MemberDeclarationSyntax declarationSyntax, IReadOnlyList<AttributeData> attributes)
    {
        return MakeDelegate(true, ctx, declarationSyntax);
    }
    
    [Inspect("UnrealSharp.Attributes.USingleDelegateAttribute", "USingleDelegateAttribute", "Global")]
    public static UnrealType? USingleDelegateAttribute(UnrealType? outer, GeneratorAttributeSyntaxContext ctx, MemberDeclarationSyntax declarationSyntax, IReadOnlyList<AttributeData> attributes)
    {
        return MakeDelegate(false, ctx, declarationSyntax);
    }

    static UnrealDelegate MakeDelegate(bool isMulticast, GeneratorAttributeSyntaxContext ctx, MemberDeclarationSyntax declarationSyntax)
    {
        ITypeSymbol typeSymbol = (ITypeSymbol)ctx.SemanticModel.GetDeclaredSymbol(declarationSyntax)!;
        UnrealDelegate unrealClass = new UnrealDelegate(isMulticast, ctx.SemanticModel, typeSymbol, ctx.TargetNode);
        return unrealClass;
    }

    public override void ExportType(GeneratorStringBuilder builder, SourceProductionContext spc)
    {
        string baseTypeName = _isMulticast ? "MulticastDelegate" : "Delegate";
        string delegateWrapperClassName = DelegateProperty.MakeDelegateSignatureName(_delegateSignature.EngineName);
        
        bool hasParameters = _delegateSignature.Properties.Count > 0;
        string args = string.Empty;
        string parameters = string.Empty;
        
        if (hasParameters)
        {
            args = string.Join(", ", _delegateSignature.Properties.Select(x => x.GetParameterDeclaration()));
            parameters = string.Join(", ", _delegateSignature.Properties.Select(x => x.GetParameterCall()));
        }
        
        builder.BeginType(_delegateSignature, TypeKind.Class, nativeTypePtrName: _delegateSignature.FunctionNativePtr, overrideTypeName: delegateWrapperClassName,  $"{baseTypeName}<{_delegateSignature.EngineName}>");
        AppendAddOperator(builder, delegateWrapperClassName);
        AppendNegateOperator(builder, delegateWrapperClassName);
        AppendInvoker(builder, args);
        builder.CloseBrace();
        builder.AppendLine();
        
        AppendExtensionsClass(builder, args, parameters);
    }

    void AppendAddOperator(GeneratorStringBuilder builder, string wrapperName)
    {
        builder.AppendLine($"public static {wrapperName} operator +({wrapperName} a, {_delegateSignature.EngineName} b)");
        builder.OpenBrace();
        builder.AppendLine("a.Add(b);");
        builder.AppendLine("return a;");
        builder.CloseBrace();
    }
    
    void AppendNegateOperator(GeneratorStringBuilder builder, string wrapperName)
    {
        builder.AppendLine($"public static {wrapperName} operator -({wrapperName} a, {_delegateSignature.EngineName} b)");
        builder.OpenBrace();
        builder.AppendLine("a.Remove(b);");
        builder.AppendLine("return a;");
        builder.CloseBrace();
    }
    
    void AppendInvoker(GeneratorStringBuilder builder, string args)
    {
        if (_delegateSignature.HasAnyProperties)
        {
            _delegateSignature.ExportBackingVariables(builder);
        }
        
        builder.AppendLine($"protected override {_delegateSignature.EngineName} GetInvoker() => Invoker;");
        
        builder.AppendLine($"private void Invoker({args})");
        builder.OpenBrace();
        _delegateSignature.ExportCallToNative(builder, (paramsbuffer, returnBuffer) =>
        {
            builder.AppendLine($"ProcessDelegate({paramsbuffer});");
        });
        builder.CloseBrace();
    }

    void AppendExtensionsClass(GeneratorStringBuilder builder, string args, string parameters)
    {
        string extensionsClassName = $"{_delegateSignature.EngineName}Extensions";
        builder.AppendLine($"public static class {extensionsClassName}");
        builder.OpenBrace();
        builder.AppendLine($"public static void Invoke(this TMulticastDelegate<{_delegateSignature.EngineName}> del{(args.Length > 0 ? ", " : string.Empty)}{args})");
        builder.Append($" => del.InnerDelegate.Invoke({parameters});");
        builder.CloseBrace();
    }
    
    public void AppendFunctionAsDelegate(GeneratorStringBuilder builder)
    {
        builder.AppendLine($"public delegate {_delegateSignature.ReturnType.ManagedType} {_delegateSignature.EngineName}({string.Join(", ", _delegateSignature.Properties.Select(x => x.GetParameterDeclaration()))});");
    }

    public override void CreateTypeBuilder(GeneratorStringBuilder builder)
    {
        base.CreateTypeBuilder(builder);
        _delegateSignature.CreateTypeBuilder(builder);
    }
}
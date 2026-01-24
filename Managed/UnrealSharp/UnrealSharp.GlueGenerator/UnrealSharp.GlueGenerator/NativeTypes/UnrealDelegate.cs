using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using UnrealSharp.GlueGenerator.NativeTypes.Properties;

namespace UnrealSharp.GlueGenerator.NativeTypes;

[Inspector]
public record UnrealDelegate : UnrealType
{
    public override string EngineName => SourceName.Substring(1);
    public override FieldType FieldType => FieldType.Delegate;
    
    private readonly UnrealFunctionBase _delegateSignature;
    private readonly bool _isMulticast;

    public UnrealDelegate(bool isMulticast, ISymbol typeSymbol) : base(typeSymbol)
    {
        INamedTypeSymbol namedTypeSymbol = (INamedTypeSymbol) typeSymbol;
        
        _isMulticast = isMulticast;
        _delegateSignature = new UnrealDelegateFunction(namedTypeSymbol.DelegateInvokeMethod!, this);
        _delegateSignature.SourceName = typeSymbol.Name;
        SourceName = DelegateProperty.MakeDelegateSignatureName(SourceName);
        
        ApplyFunctionFlags(isMulticast);
    }

    public UnrealDelegate(UnrealFunctionBase delegateSignature, bool isMulticast) : base(delegateSignature.SourceName, delegateSignature.Namespace, Accessibility.Public, delegateSignature.AssemblyName, delegateSignature.Outer)
    {
        _isMulticast = isMulticast;
        _delegateSignature = delegateSignature;
        SourceName = DelegateProperty.MakeDelegateSignatureName(SourceName);
        
        ApplyFunctionFlags(isMulticast);
    }
    
    void ApplyFunctionFlags(bool isMulticast)
    {
        _delegateSignature.FunctionFlags |= EFunctionFlags.Delegate;
        
        if (isMulticast)
        {
            _delegateSignature.FunctionFlags |= EFunctionFlags.MulticastDelegate;
        }
    }
    
    [Inspect("UnrealSharp.Attributes.UMultiDelegateAttribute", "UMultiDelegateAttribute", "Global")]
    public static UnrealType UMultiDelegateAttribute(UnrealType? outer, SyntaxNode? syntaxNode, GeneratorAttributeSyntaxContext ctx, ISymbol symbol, IReadOnlyList<AttributeData> attributes)
    {
        return MakeDelegate(true, symbol);
    }
    
    [Inspect("UnrealSharp.Attributes.USingleDelegateAttribute", "USingleDelegateAttribute", "Global")]
    public static UnrealType USingleDelegateAttribute(UnrealType? outer, SyntaxNode? syntaxNode, GeneratorAttributeSyntaxContext ctx, ISymbol symbol, IReadOnlyList<AttributeData> attributes)
    {
        return MakeDelegate(false, symbol);
    }

    static UnrealDelegate MakeDelegate(bool isMulticast, ISymbol symbol)
    {
        UnrealDelegate unrealClass = new UnrealDelegate(isMulticast, symbol);
        return unrealClass;
    }

    public override void ExportType(GeneratorStringBuilder builder, SourceProductionContext spc)
    {
        string baseTypeName = _isMulticast ? "MulticastDelegate" : "Delegate";
        string delegateWrapperClassName = DelegateProperty.MakeDelegateSignatureName(_delegateSignature.SourceName);
        
        bool hasParameters = _delegateSignature.Properties.Count > 0;
        string args = string.Empty;
        string parameters = string.Empty;
        
        if (hasParameters)
        {
            args = string.Join(", ", _delegateSignature.Properties.Select(x => x.GetParameterDeclaration()));
            parameters = string.Join(", ", _delegateSignature.Properties.Select(x => x.GetParameterCall()));
        }
        
        builder.BeginType(_delegateSignature, SourceGenUtilities.ClassKeyword, null, nativeTypePtrName: _delegateSignature.FunctionNativePtr, overrideTypeName: delegateWrapperClassName,  $"{baseTypeName}<{_delegateSignature.SourceName}>");

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
        
        builder.AppendLine($"protected override {_delegateSignature.SourceName} GetInvoker() => Invoker;");
        
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
        builder.AppendLine($"public static void Invoke(this TMulticastDelegate<{_delegateSignature.SourceName}> del{(args.Length > 0 ? ", " : string.Empty)}{args})");
        builder.Append($" => del.InnerDelegate.Invoke({parameters});");
        builder.CloseBrace();
    }
    
    public void AppendFunctionAsDelegate(GeneratorStringBuilder builder)
    {
        builder.AppendLine($"public delegate {_delegateSignature.ReturnType.ManagedType} {_delegateSignature.SourceName}({string.Join(", ", _delegateSignature.Properties.Select(x => x.GetParameterDeclaration()))});");
    }

    public override void PopulateJsonObject(JsonWriter jsonWriter)
    {
        _delegateSignature.PopulateJsonObject(jsonWriter);
        base.PopulateJsonObject(jsonWriter);
    }
}
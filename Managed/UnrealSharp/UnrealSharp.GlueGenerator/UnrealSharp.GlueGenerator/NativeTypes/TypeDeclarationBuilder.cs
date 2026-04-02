using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes;

public class TypeDeclarationBuilder
{
    private readonly string _typeKeyword;
    private string? _modifiers;
    private string? _baseType;
    private string? _accessibility;
    private readonly List<string> _interfaces = [];
    private string _nativeTypePtrName = SourceGenUtilities.NativeTypePtr;
    
    private string _engineName = string.Empty;
    private string _namespace = string.Empty;
    private string _declarationName = string.Empty;
    private string _assemblyName = string.Empty;
    private readonly string _defaultAccessibility = string.Empty;

    private TypeDeclarationBuilder(string typeKeyword)
    {
        _typeKeyword = typeKeyword;
    }

    public static TypeDeclarationBuilder Create(string typeKeyword)
    {
        return new TypeDeclarationBuilder(typeKeyword);
    }
    
    public static TypeDeclarationBuilder FromUnrealType(UnrealType type, string typeKeyword)
    {
        return Create(typeKeyword)
            .WithEngineName(type.EngineName)
            .WithNamespace(type.Namespace)
            .WithDeclarationName(type.SourceName)
            .WithAssemblyName(type.AssemblyName)
            .Accessibility(type.TypeAccessibility.AccessibilityToString());
    }

    public TypeDeclarationBuilder WithEngineName(string engineName)
    {
        _engineName = engineName;
        return this;
    }

    public TypeDeclarationBuilder WithNamespace(string @namespace)
    {
        _namespace = @namespace;
        return this;
    }

    public TypeDeclarationBuilder WithDeclarationName(string name)
    {
        _declarationName = name;
        return this;
    }

    public TypeDeclarationBuilder WithAssemblyName(string assemblyName)
    {
        _assemblyName = assemblyName;
        return this;
    }

    public TypeDeclarationBuilder WithModifiers(string modifiers)
    {
        _modifiers = modifiers;
        return this;
    }

    public TypeDeclarationBuilder Extends(string baseType)
    {
        _baseType = baseType;
        return this;
    }

    public TypeDeclarationBuilder Implements(params string[] interfaces)
    {
        _interfaces.AddRange(interfaces);
        return this;
    }

    public TypeDeclarationBuilder WithNativePtr(string name)
    {
        _nativeTypePtrName = name;
        return this;
    }

    public TypeDeclarationBuilder Accessibility(Accessibility accessibility)
    {
        _accessibility = accessibility.AccessibilityToString();
        return this;
    }

    public TypeDeclarationBuilder Accessibility(string accessibility)
    {
        _accessibility = accessibility;
        return this;
    }

    public void Build(GeneratorStringBuilder builder)
    {
        string protection = _accessibility ?? _defaultAccessibility;
        builder.AppendLine($"[GeneratedType(\"{_engineName}\", \"{_namespace}.{_engineName}\")]");
        builder.AppendLine($"{protection}partial {_modifiers}{_typeKeyword} {_declarationName}");

        List<string> inheritance = _baseType is not null ? [_baseType, .._interfaces] : _interfaces;

        if (inheritance.Count > 0)
        {
            builder.Append($" : {string.Join(", ", inheritance)}");
        }

        builder.OpenBrace();
        builder.AppendNewBackingField($"static IntPtr {_nativeTypePtrName} = UCoreUObjectExporter.CallGetType(\"{_assemblyName}\", \"{_namespace}\", \"{_engineName}\");");
    }
}
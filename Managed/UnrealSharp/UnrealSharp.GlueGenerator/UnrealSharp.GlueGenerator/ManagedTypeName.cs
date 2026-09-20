using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using UnrealSharp.GlueGenerator.NativeTypes;

namespace UnrealSharp.GlueGenerator;

public readonly record struct ManagedTypeName
{
    private static readonly SymbolDisplayFormat DisplayFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);

    public readonly string Text;

    public ManagedTypeName(string text)
    {
        Text = text;
    }

    public static ManagedTypeName FromSymbol(ITypeSymbol typeSymbol)
    {
        ITypeSymbol canonical = typeSymbol.WithNullableAnnotation(NullableAnnotation.None);
        return new ManagedTypeName(canonical.ToDisplayString(DisplayFormat));
    }

    public static ManagedTypeName Generic(string openTypeName, IEnumerable<string> typeArguments)
    {
        return new ManagedTypeName($"{openTypeName}<{string.Join(", ", typeArguments)}>");
    }

    public static ManagedTypeName FromFieldName(FieldName fieldName)
    {
        return new ManagedTypeName(fieldName.FullName);
    }

    public string FullName => Text;
    public override string ToString() => Text;
}
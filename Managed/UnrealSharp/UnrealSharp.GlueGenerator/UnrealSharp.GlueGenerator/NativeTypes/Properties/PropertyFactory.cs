using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public static class PropertyFactory
{
    private static readonly Dictionary<string, Func<SyntaxNode, ISymbol, ITypeSymbol, UnrealType, UnrealProperty>> Factories = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Byte"] = (s, m, t, o) => new NumericProperty(s, m, t, PropertyType.Byte, o),
        ["SByte"] = (s, m, t, o) => new NumericProperty(s, m, t, PropertyType.Int8, o),
        ["Int16"] = (s, m, t, o) => new NumericProperty(s, m, t, PropertyType.Int16, o),
        ["UInt16"] = (s, m, t, o) => new NumericProperty(s, m, t, PropertyType.UInt16, o),
        ["Int32"] = (s, m, t, o) => new NumericProperty(s, m, t, PropertyType.Int, o),
        ["UInt32"] = (s, m, t, o) => new NumericProperty(s, m, t, PropertyType.UInt32, o),
        ["Int64"] = (s, m, t, o) => new NumericProperty(s, m, t, PropertyType.Int64, o),
        ["UInt64"] = (s, m, t, o) => new NumericProperty(s, m, t, PropertyType.UInt64, o),
        ["Single"] = (s, m, t, o) => new NumericProperty(s, m, t, PropertyType.Float, o),
        ["Double"] = (s, m, t, o) => new NumericProperty(s, m, t, PropertyType.Double, o),

        ["Boolean"] = (s, m, t, o) => new BoolProperty(s, m, t, o),
        ["String"] = (s, m, t, o) => new StringProperty(s, m, t, o),

        ["TSubclassOf"] = (s, m, t, o) => new ClassProperty(s, m, t, o),
        ["TSoftObjectPtr"] = (s, m, t, o) => new SoftObjectProperty(s, m, t, o),
        ["TSoftClassPtr"] = (s, m, t, o) => new SoftClassProperty(s, m, t, o),
        ["TMulticastDelegate"] = (s, m, t, o) => new MulticastDelegateProperty(s, m, t, o),
        ["TDelegate"] = (s, m, t, o) => new SingleDelegateProperty(s, m, t, o),
        ["Option"] = (s, m, t, o) => new OptionProperty(s, m, t, o),
        ["FText"] = (s, m, t, o) => new TextProperty(s, m, t, o),
        ["TWeakObjectPtr"] = (s, m, t, o) => new WeakObjectProperty(s, m, t, o),
        ["FName"] = (s, m, t, o) => new NameProperty(s, m, t, o),

        ["TArray"] = (s, m, t, o) => new ArrayProperty(s, m, t, o),
        ["List"] = (s, m, t, o) => new ArrayProperty(s, m, t, o),
        ["IList"] = (s, m, t, o) => new ArrayProperty(s, m, t, o),
        ["IEnumerable"] = (s, m, t, o) => new ArrayProperty(s, m, t, o),
        ["ICollection"] = (s, m, t, o) => new ArrayProperty(s, m, t, o),

        ["TMap"] = (s, m, t, o) => new MapProperty(s, m, t, o),
        ["IDictionary"] = (s, m, t, o) => new MapProperty(s, m, t, o),

        ["TSet"] = (s, m, t, o) => new SetProperty(s, m, t, o),
        ["ISet"] = (s, m, t, o) => new SetProperty(s, m, t, o)
    };

    public static UnrealProperty CreateProperty(SemanticModel model, SyntaxNode syntax, UnrealType outer)
    {
        ITypeSymbol GetMemberType(ISymbol symbol) => symbol switch
        {
            IPropertySymbol p => p.Type,
            IFieldSymbol f => f.Type,
            IParameterSymbol tp => tp.Type,
            _ => throw new NotSupportedException($"Unsupported symbol: {symbol.Kind}")
        };
        
        if (syntax is FieldDeclarationSyntax fieldDecl)
        {
            VariableDeclaratorSyntax variable = fieldDecl.Declaration.Variables.First();
            ISymbol symbol = model.GetDeclaredSymbol(variable)!;
            ITypeSymbol memberType = GetMemberType(symbol);
            return CreateProperty(memberType, fieldDecl, symbol, outer);
        }
        else
        {
            ISymbol memberSymbol = model.GetDeclaredSymbol(syntax)!;
            ITypeSymbol memberType = GetMemberType(memberSymbol);
            return CreateProperty(memberType, syntax, memberSymbol, outer);
        }
    }

    public static UnrealProperty CreateProperty(ITypeSymbol typeSymbol, SyntaxNode syntaxNode, ISymbol memberSymbol, UnrealType outer)
    {
        if (Factories.TryGetValue(typeSymbol.Name, out var factory))
        {
            return factory(syntaxNode, memberSymbol, typeSymbol, outer);
        }
        
        return typeSymbol.TypeKind switch
        {
            TypeKind.Class => new ObjectProperty(syntaxNode, memberSymbol, typeSymbol, outer),
            TypeKind.Interface => new InterfaceProperty(syntaxNode, memberSymbol, typeSymbol, outer),
            TypeKind.Enum => new EnumProperty(syntaxNode, memberSymbol, typeSymbol, outer),
            TypeKind.Struct => typeSymbol.HasAttribute("BlittableTypeAttribute") 
                ? new BlittableStructProperty(syntaxNode, memberSymbol, typeSymbol, PropertyType.Struct, outer) 
                : new StructProperty(syntaxNode, memberSymbol, typeSymbol, outer),
            _ => throw new NotSupportedException($"Type {typeSymbol} is not supported in PropertyFactory")
        };
    }
}
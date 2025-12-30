using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public static class PropertyFactory
{
    private static readonly Dictionary<string, Func<ISymbol, ITypeSymbol, UnrealType, SyntaxNode?, UnrealProperty>> Factories = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Byte"] = (m, t, o, syntaxNode) => new NumericProperty(m, t, PropertyType.Byte, o, syntaxNode),
        ["SByte"] = (m, t, o, syntaxNode) => new NumericProperty(m, t, PropertyType.Int8, o, syntaxNode),
        ["Int16"] = (m, t, o, syntaxNode) => new NumericProperty(m, t, PropertyType.Int16, o, syntaxNode),
        ["UInt16"] = (m, t, o, syntaxNode) => new NumericProperty(m, t, PropertyType.UInt16, o, syntaxNode),
        ["Int32"] = (m, t, o, syntaxNode) => new NumericProperty(m, t, PropertyType.Int, o, syntaxNode),
        ["UInt32"] = (m, t, o, syntaxNode) => new NumericProperty(m, t, PropertyType.UInt32, o, syntaxNode),
        ["Int64"] = (m, t, o, syntaxNode) => new NumericProperty(m, t, PropertyType.Int64, o, syntaxNode),
        ["UInt64"] = (m, t, o, syntaxNode) => new NumericProperty(m, t, PropertyType.UInt64, o, syntaxNode),
        ["Single"] = (m, t, o, syntaxNode) => new NumericProperty(m, t, PropertyType.Float, o, syntaxNode),
        ["Double"] = (m, t, o, syntaxNode) => new NumericProperty(m, t, PropertyType.Double, o, syntaxNode),

        ["Boolean"] = (m, t, o, syntaxNode) => new BoolProperty(m, t, o, syntaxNode),
        ["String"] = (m, t, o, syntaxNode) => new StringProperty(m, t, o, syntaxNode),

        ["TSubclassOf"] = (m, t, o, syntaxNode) => new ClassProperty(m, t, o, syntaxNode),
        ["TSoftObjectPtr"] = (m, t, o, syntaxNode) => new SoftObjectProperty(m, t, o, syntaxNode),
        ["TSoftClassPtr"] = (m, t, o, syntaxNode) => new SoftClassProperty(m, t, o, syntaxNode),
        ["TMulticastDelegate"] = (m, t, o, syntaxNode) => new MulticastDelegateProperty(m, t, o, syntaxNode),
        ["TDelegate"] = (m, t, o, syntaxNode) => new SingleDelegateProperty(m, t, o, syntaxNode),
        ["Option"] = (m, t, o, syntaxNode) => new OptionProperty(m, t, o, syntaxNode),
        ["FText"] = (m, t, o, syntaxNode) => new TextProperty(m, t, o, syntaxNode),
        ["TWeakObjectPtr"] = (m, t, o, syntaxNode) => new WeakObjectProperty(m, t, o, syntaxNode),
        ["FName"] = (m, t, o, syntaxNode) => new NameProperty(m, t, o, syntaxNode),

        ["TArray"] = (m, t, o, syntaxNode) => new ArrayProperty(m, t, o, syntaxNode),
        ["List"] = (m, t, o, syntaxNode) => new ArrayProperty(m, t, o, syntaxNode),
        ["IList"] = (m, t, o, syntaxNode) => new ArrayProperty(m, t, o, syntaxNode),
        ["IEnumerable"] = (m, t, o, syntaxNode) => new ArrayProperty(m, t, o, syntaxNode),
        ["ICollection"] = (m, t, o, syntaxNode) => new ArrayProperty(m, t, o, syntaxNode),
        
        ["TNativeArray"] = (m, t, o, syntaxNode) => new NativeArrayProperty(m, t, o, syntaxNode),
        ["Span"] = (m, t, o, syntaxNode) => new NativeArrayProperty(m, t, o, syntaxNode),
        ["ReadOnlySpan"] = (m, t, o, syntaxNode) => new NativeArrayProperty(m, t, o, syntaxNode),

        ["TMap"] = (m, t, o, syntaxNode) => new MapProperty(m, t, o, syntaxNode),
        ["IDictionary"] = (m, t, o, syntaxNode) => new MapProperty(m, t, o, syntaxNode),

        ["TSet"] = (m, t, o, syntaxNode) => new SetProperty(m, t, o, syntaxNode),
        ["ISet"] = (m, t, o, syntaxNode) => new SetProperty(m, t, o, syntaxNode),
        
        ["ValueTask"] = (m, t, o, syntaxNode) => new ValueTaskProperty(m, t, o, syntaxNode),
        ["Task"] = (m, t, o, syntaxNode) => new TaskProperty(m, t, o, syntaxNode),
    };

    public static UnrealProperty CreateProperty(ISymbol memberSymbol, UnrealType outer, SyntaxNode? syntaxNode = null)
    {
        ITypeSymbol GetMemberType(ISymbol symbol) => symbol switch
        {
            IPropertySymbol p => p.Type,
            IFieldSymbol f => f.Type,
            IParameterSymbol tp => tp.Type,
            _ => throw new NotSupportedException($"Unsupported symbol: {symbol.Kind}")
        };
        
        ISymbol GetAssociatedSymbol(ISymbol symbol)
        {
            if (symbol is IFieldSymbol fieldSymbol)
            {
                return fieldSymbol.AssociatedSymbol ?? fieldSymbol;
            }
            
            return symbol;
        }
        
        ITypeSymbol innerTypeSymbol = GetMemberType(memberSymbol);
        ISymbol newMemberSymbol = GetAssociatedSymbol(memberSymbol);
        
        return CreateProperty(innerTypeSymbol, newMemberSymbol, outer, syntaxNode);
    }

    public static UnrealProperty CreateProperty(ITypeSymbol? typeSymbol, ISymbol memberSymbol, UnrealType outer, SyntaxNode? syntaxNode = null)
    {
        if (typeSymbol == null)
        {
            return new VoidProperty(outer);
        }
        
        if (Factories.TryGetValue(typeSymbol.Name, out Func<ISymbol, ITypeSymbol, UnrealType, SyntaxNode?, UnrealProperty>? factory))
        {
            return factory(memberSymbol, typeSymbol, outer, syntaxNode);
        }
        
        return typeSymbol.TypeKind switch
        {
            TypeKind.Delegate => new FieldProperty(memberSymbol, DelegateProperty.MakeFieldNameFromDelegateSymbol(typeSymbol), typeSymbol, PropertyType.SignatureDelegate, outer, syntaxNode),
            TypeKind.Class => new ObjectProperty(memberSymbol, typeSymbol, outer, syntaxNode),
            TypeKind.Interface => new InterfaceProperty(memberSymbol, typeSymbol, outer, syntaxNode),
            TypeKind.Enum => new EnumProperty(memberSymbol, typeSymbol, outer, syntaxNode),
            TypeKind.Struct => typeSymbol.HasAttribute("BlittableTypeAttribute") ? new BlittableStructProperty(memberSymbol, typeSymbol, PropertyType.Struct, outer, syntaxNode) : new StructProperty(memberSymbol, typeSymbol, outer, syntaxNode),
            _ => throw new NotSupportedException($"Type {typeSymbol} is not supported in PropertyFactory")
        };
    }
}
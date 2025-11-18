using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public static class PropertyFactory
{
    private static readonly Dictionary<string, Func<ISymbol, ITypeSymbol, UnrealType, UnrealProperty>> Factories = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Byte"] = (m, t, o) => new NumericProperty(m, t, PropertyType.Byte, o),
        ["SByte"] = (m, t, o) => new NumericProperty(m, t, PropertyType.Int8, o),
        ["Int16"] = (m, t, o) => new NumericProperty(m, t, PropertyType.Int16, o),
        ["UInt16"] = (m, t, o) => new NumericProperty(m, t, PropertyType.UInt16, o),
        ["Int32"] = (m, t, o) => new NumericProperty(m, t, PropertyType.Int, o),
        ["UInt32"] = (m, t, o) => new NumericProperty(m, t, PropertyType.UInt32, o),
        ["Int64"] = (m, t, o) => new NumericProperty(m, t, PropertyType.Int64, o),
        ["UInt64"] = (m, t, o) => new NumericProperty(m, t, PropertyType.UInt64, o),
        ["Single"] = (m, t, o) => new NumericProperty(m, t, PropertyType.Float, o),
        ["Double"] = (m, t, o) => new NumericProperty(m, t, PropertyType.Double, o),

        ["Boolean"] = (m, t, o) => new BoolProperty(m, t, o),
        ["String"] = (m, t, o) => new StringProperty(m, t, o),

        ["TSubclassOf"] = (m, t, o) => new ClassProperty(m, t, o),
        ["TSoftObjectPtr"] = (m, t, o) => new SoftObjectProperty(m, t, o),
        ["TSoftClassPtr"] = (m, t, o) => new SoftClassProperty(m, t, o),
        ["TMulticastDelegate"] = (m, t, o) => new MulticastDelegateProperty(m, t, o),
        ["TDelegate"] = (m, t, o) => new SingleDelegateProperty(m, t, o),
        ["Option"] = (m, t, o) => new OptionProperty(m, t, o),
        ["FText"] = (m, t, o) => new TextProperty(m, t, o),
        ["TWeakObjectPtr"] = (m, t, o) => new WeakObjectProperty(m, t, o),
        ["FName"] = (m, t, o) => new NameProperty(m, t, o),

        ["TArray"] = (m, t, o) => new ArrayProperty(m, t, o),
        ["List"] = (m, t, o) => new ArrayProperty(m, t, o),
        ["IList"] = (m, t, o) => new ArrayProperty(m, t, o),
        ["IEnumerable"] = (m, t, o) => new ArrayProperty(m, t, o),
        ["ICollection"] = (m, t, o) => new ArrayProperty(m, t, o),

        ["TMap"] = (m, t, o) => new MapProperty(m, t, o),
        ["IDictionary"] = (m, t, o) => new MapProperty(m, t, o),

        ["TSet"] = (m, t, o) => new SetProperty(m, t, o),
        ["ISet"] = (m, t, o) => new SetProperty(m, t, o)
    };

    public static UnrealProperty CreateProperty(SemanticModel model, ISymbol memberSymbol, UnrealType outer)
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
        
        return CreateProperty(innerTypeSymbol, newMemberSymbol, outer);
    }

    public static UnrealProperty CreateProperty(ITypeSymbol typeSymbol, ISymbol memberSymbol, UnrealType outer)
    {
        if (Factories.TryGetValue(typeSymbol.Name, out Func<ISymbol, ITypeSymbol, UnrealType, UnrealProperty>? factory))
        {
            return factory(memberSymbol, typeSymbol, outer);
        }
        
        return typeSymbol.TypeKind switch
        {
            TypeKind.Delegate => new FieldProperty(memberSymbol, DelegateProperty.MakeFieldNameFromDelegateSymbol(typeSymbol), typeSymbol, PropertyType.SignatureDelegate, outer),
            TypeKind.Class => new ObjectProperty(memberSymbol, typeSymbol, outer),
            TypeKind.Interface => new InterfaceProperty(memberSymbol, typeSymbol, outer),
            TypeKind.Enum => new EnumProperty(memberSymbol, typeSymbol, outer),
            TypeKind.Struct => typeSymbol.HasAttribute("BlittableTypeAttribute") ? new BlittableStructProperty(memberSymbol, typeSymbol, PropertyType.Struct, outer)  : new StructProperty(memberSymbol, typeSymbol, outer),
            _ => throw new NotSupportedException($"Type {typeSymbol} is not supported in PropertyFactory")
        };
    }
}
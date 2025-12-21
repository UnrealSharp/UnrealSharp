using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace UnrealSharp.GlueGenerator.NativeTypes;

[Inspector]
public record UnrealEnum : UnrealType
{
    public override string EngineName => SourceName.Substring(1);
    public override FieldType FieldType => FieldType.Enum;

    private readonly EquatableList<string>? _enumNames;

    public UnrealEnum(ITypeSymbol symbol, UnrealType? outer = null) : base(symbol, outer)
    {
        ImmutableArray<ISymbol> members = symbol.GetMembers();
        
        if (members.Length == 0)
        {
            return;
        }
        
        List<string> enumMembers = new List<string>(members.Length - 1);
        
        for (int i = 0; i < members.Length; i++)
        {
            ISymbol member = members[i];
            
            if (member.Kind != SymbolKind.Field)
            {
                continue;
            }
            
            enumMembers.Add(member.Name);
        }
        
        _enumNames = new EquatableList<string>(enumMembers);
    }
    
    public UnrealEnum(EquatableList<string> names, string sourceName, string typeNameSpace, Accessibility accessibility, string assemblyName, UnrealType? outer = null) 
        : base(sourceName, typeNameSpace, accessibility, assemblyName, outer)
    {
        _enumNames = names;
    }
    
    [Inspect("UnrealSharp.Attributes.UEnumAttribute", "UEnumAttribute", "Global")]
    public static UnrealType? UEnumAttribute(UnrealType? outer, SyntaxNode? syntaxNode, GeneratorAttributeSyntaxContext ctx, ISymbol symbol, IReadOnlyList<AttributeData> attributes)
    {
        return new UnrealEnum((ITypeSymbol) symbol, outer);
    }

    public override void PopulateJsonObject(JsonWriter jsonWriter)
    {
        base.PopulateJsonObject(jsonWriter);
        
        if (_enumNames is null)
        {
            return;
        }

        jsonWriter.WritePropertyName("EnumNames");
        jsonWriter.WriteStartArray();
        foreach (string name in _enumNames)
        {
            jsonWriter.WriteValue(name);
        }
        jsonWriter.WriteEndArray();
    }
}
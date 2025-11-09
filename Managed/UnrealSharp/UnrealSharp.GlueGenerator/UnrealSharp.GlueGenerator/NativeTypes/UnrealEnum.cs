using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnrealSharp.GlueGenerator.NativeTypes;

[Inspector]
public record UnrealEnum : UnrealType
{
    public override string EngineName => SourceName.Substring(1);
    public override int FieldTypeValue => 2;

    private readonly EquatableList<string>? _enumNames;

    public UnrealEnum(SyntaxNode syntax, ITypeSymbol typeSymbol, UnrealType? outer = null) : base(typeSymbol, syntax, outer)
    {
        ImmutableArray<ISymbol> members = typeSymbol.GetMembers();
        
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
    public static UnrealType? UEnumAttribute(UnrealType? outer, GeneratorAttributeSyntaxContext ctx, MemberDeclarationSyntax declarationSyntax, IReadOnlyList<AttributeData> attributes)
    {
        ITypeSymbol typeSymbol = (ITypeSymbol) ctx.SemanticModel.GetDeclaredSymbol(declarationSyntax)!;
        UnrealEnum unrealEnum = new UnrealEnum(declarationSyntax, typeSymbol, outer);
        return unrealEnum;
    }

    public override void PopulateJsonObject(JsonObject jsonObject)
    {
        base.PopulateJsonObject(jsonObject);
        
        if (_enumNames is null)
        {
            return;
        }
        
        JsonArray enumNamesArray = new JsonArray();
        foreach (string name in _enumNames)
        {
            enumNamesArray.Add(name);
        }
        
        jsonObject["EnumNames"] = enumNamesArray;
    }
}
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace UnrealSharp.GlueGenerator.NativeTypes;

[Inspector]
public record UnrealEnum : UnrealType
{
    public override FieldType FieldType => FieldType.Enum;
    public readonly EquatableList<string> EnumNames;

    public UnrealEnum(ITypeSymbol symbol, UnrealType? outer = null) : base(symbol, outer)
    {
        ImmutableArray<ISymbol> members = symbol.GetMembers();
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

        EnumNames = new EquatableList<string>(enumMembers);
    }

    public UnrealEnum(EquatableList<string> names, string sourceName, string typeNameSpace, Accessibility accessibility,
        string assemblyName, UnrealType? outer = null)
        : base(sourceName, typeNameSpace, accessibility, assemblyName, outer)
    {
        EnumNames = names;
    }

    [Inspect("UnrealSharp.Attributes.UEnumAttribute", "UEnumAttribute", "Global")]
    public static UnrealType UEnumAttribute(UnrealType? outer, SyntaxNode? syntaxNode,
        GeneratorAttributeSyntaxContext ctx, ISymbol symbol, IReadOnlyList<AttributeData> attributes)
    {
        return new UnrealEnum((ITypeSymbol)symbol, outer);
    }

    public override void ExportType(GeneratorStringBuilder builder, SourceProductionContext spc)
    {
        builder.GenerateTypeRegistration(this);
    }

    public override void PopulateJsonObject(JsonWriter jsonWriter)
    {
        base.PopulateJsonObject(jsonWriter);

        jsonWriter.WritePropertyName("EnumNames");
        jsonWriter.WriteStartArray();
        foreach (string name in EnumNames)
        {
            jsonWriter.WriteValue(name);
        }

        jsonWriter.WriteEndArray();
    }
}
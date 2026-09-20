using System.Linq;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record TemplateProperty : UnrealProperty
{
    public readonly EquatableArray<UnrealProperty> TemplateParameters;

    public override string MarshallerType
    {
        get
        {
            if (!HasTemplateParameters)
            {
                return string.Empty;
            }

            return MakeMarshallerType(field, TemplateParameters.Select(t => t.ManagedType.Text).ToArray());
        }
    }

    public bool HasTemplateParameters => TemplateParameters.Count > 0;

    private static readonly SymbolDisplayFormat NoTypeArgumentsFormat =
        SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)
            .WithGenericsOptions(SymbolDisplayGenericsOptions.None);

    public TemplateProperty(ISymbol symbol, ITypeSymbol typeSymbol, PropertyType propertyType, UnrealType outer,
        string marshaller, SyntaxNode? syntaxNode = null)
        : base(symbol, typeSymbol, propertyType, outer, syntaxNode)
    {
        MarshallerType = marshaller;
        INamedTypeSymbol namedTypeSymbol = (INamedTypeSymbol)typeSymbol!;

        int argumentCount = namedTypeSymbol.TypeArguments.Length;
        UnrealProperty[] arguments = new UnrealProperty[argumentCount];

        for (int i = 0; i < argumentCount; i++)
        {
            ITypeSymbol argumentSymbol = namedTypeSymbol.TypeArguments[i];
            UnrealProperty newArgument = PropertyFactory.CreateProperty(argumentSymbol, argumentSymbol, this);

            newArgument.FieldName = FieldName.Member($"{FieldName.SourceName}_Arg{i}");

            arguments[i] = newArgument;
        }

        TemplateParameters = new EquatableArray<UnrealProperty>(arguments);

        if (HasTemplateParameters)
        {
            ManagedType = ManagedTypeName.Generic(
                namedTypeSymbol.ConstructedFrom.ToDisplayString(NoTypeArgumentsFormat),
                TemplateParameters.Select(t => $"{t.ManagedType}{t.GetNullableAnnotation()}"));
        }
        else
        {
            ManagedType = ManagedTypeName.FromSymbol(namedTypeSymbol);
        }
    }

    public TemplateProperty(EquatableArray<UnrealProperty> templateParameters, ManagedTypeName openType,
        PropertyType propertyType, string marshaller, string sourceName, Accessibility accessibility, UnrealType outer)
        : base(propertyType, sourceName, accessibility, outer)
    {
        MarshallerType = marshaller;
        TemplateParameters = templateParameters;
        ManagedType = ManagedTypeName.Generic(openType.Text, TemplateParameters.Select(t => t.ManagedType.Text));
    }

    public string MakeMarshallerType(string marshallerName, params string[] innerTypes)
    {
        return $"{marshallerName}<{string.Join(", ", innerTypes)}>";
    }

    public override void CollectDependencies()
    {
        base.CollectDependencies();

        foreach (UnrealProperty parameter in TemplateParameters)
        {
            parameter.CollectDependencies();

            if (parameter is FieldProperty fieldParameter)
            {
                Outer?.AddDependency(fieldParameter.InnerType);
            }
        }
    }

    public override void PopulateJsonObject(JsonWriter jsonWriter)
    {
        base.PopulateJsonObject(jsonWriter);
        TemplateParameters.PopulateJsonWithArray(jsonWriter, JsonKeys.TemplateParameters);
    }
}
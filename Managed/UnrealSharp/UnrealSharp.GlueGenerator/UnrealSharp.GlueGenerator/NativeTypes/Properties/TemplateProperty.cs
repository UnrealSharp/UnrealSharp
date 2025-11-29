using System;
using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record TemplateProperty : UnrealProperty
{
    protected readonly EquatableArray<UnrealProperty> TemplateParameters;
    private readonly string _marshallerName;
    
    public override string MarshallerType => MakeMarshallerType(_marshallerName, TemplateParameters.Select(t => t.ManagedType.FullName).ToArray());

    public TemplateProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, PropertyType propertyType, UnrealType outer, string marshaller, SyntaxNode? syntaxNode = null)
        : base(memberSymbol, typeSymbol, propertyType, outer, syntaxNode)
    {
        _marshallerName = marshaller;
        INamedTypeSymbol namedTypeSymbol = (INamedTypeSymbol) typeSymbol!;
        
        int argumentCount = namedTypeSymbol.TypeArguments.Length;
        UnrealProperty[] arguments = new UnrealProperty[argumentCount];
        
        for (int i = 0; i < argumentCount; i++)
        {
            ITypeSymbol argumentSymbol = namedTypeSymbol.TypeArguments[i];
            UnrealProperty newArgument = PropertyFactory.CreateProperty(argumentSymbol, argumentSymbol, this);
            newArgument.SourceName = $"{SourceName}_Arg{i}";
            arguments[i] = newArgument;
        }
        
        TemplateParameters = new EquatableArray<UnrealProperty>(arguments);
        
        string fullNamespace = namedTypeSymbol.ContainingNamespace.ToDisplayString();
        string typedArguments = string.Join(", ", TemplateParameters.Select(t => t.ManagedType));
        ManagedType = new FieldName($"{namedTypeSymbol.Name}<{typedArguments}>", fullNamespace, namedTypeSymbol.ContainingAssembly.Name);
    }
    
    public TemplateProperty(EquatableArray<UnrealProperty> templateParameters, FieldName fieldName, PropertyType propertyType, string marshaller, string sourceName, Accessibility accessibility, UnrealType outer) 
        : base(propertyType, sourceName, accessibility, outer)
    {
        _marshallerName = marshaller;
        TemplateParameters = templateParameters;
        
        string typedArguments = string.Join(", ", TemplateParameters.Select(t => t.ManagedType));
        ManagedType = new FieldName($"{fieldName.Name}<{typedArguments}>", "UnrealSharp", outer.AssemblyName);
    }
    
    public string MakeMarshallerType(string marshallerName, params string[] innerTypes)
    {
        return $"{marshallerName}<{string.Join(", ", innerTypes)}>";
    }

    public override void PopulateJsonObject(JsonObject jsonObject)
    {
        base.PopulateJsonObject(jsonObject);
        TemplateParameters.PopulateJsonWithArray(jsonObject, "TemplateParameters");
    }
}
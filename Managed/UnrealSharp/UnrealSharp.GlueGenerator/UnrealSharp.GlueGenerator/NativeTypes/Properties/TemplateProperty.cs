using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record TemplateProperty : UnrealProperty
{
    protected readonly EquatableArray<UnrealProperty> InnerTypes;
    private readonly string _marshallerName;
    
    public override string MarshallerType => MakeMarshallerType(_marshallerName, InnerTypes.Select(t => t.ManagedType).ToArray());

    public TemplateProperty(SyntaxNode syntaxNode, ISymbol memberSymbol, ITypeSymbol? typeSymbol, PropertyType propertyType, UnrealType outer, string marshaller)
        : base(syntaxNode, memberSymbol, typeSymbol, propertyType, outer)
    {
        CacheNativeTypePtr = true;
        
        _marshallerName = marshaller;
        INamedTypeSymbol namedTypeSymbol = (INamedTypeSymbol) typeSymbol!;
        
        int argumentCount = namedTypeSymbol.TypeArguments.Length;
        UnrealProperty[] arguments = new UnrealProperty[argumentCount];
        
        TypeSyntax variableDeclarationSyntax;
        if (syntaxNode is BasePropertyDeclarationSyntax propertyDeclarationSyntax)
        {
            variableDeclarationSyntax = propertyDeclarationSyntax.Type;
        }
        else if (syntaxNode is FieldDeclarationSyntax fieldDeclarationSyntax)
        {
            variableDeclarationSyntax = fieldDeclarationSyntax.Declaration.Type;
        }
        else if (syntaxNode is GenericNameSyntax genericNameSyntax)
        {
            variableDeclarationSyntax = genericNameSyntax;
        }
        else
        {
            throw new InvalidOperationException($"Unsupported syntax node type: {syntaxNode.GetType().Name}");
        }
        
        GenericNameSyntax nameSyntax = (GenericNameSyntax) variableDeclarationSyntax;
        
        for (int i = 0; i < argumentCount; i++)
        {
            TypeSyntax argumentSyntax = nameSyntax.TypeArgumentList.Arguments[i];
            ITypeSymbol argumentSymbol = namedTypeSymbol.TypeArguments[i];
            UnrealProperty newArgument = PropertyFactory.CreateProperty(argumentSymbol, argumentSyntax, argumentSymbol, this);
            newArgument.SourceName = $"{SourceName}_Arg{i}";
            arguments[i] = newArgument;
        }
        
        InnerTypes = new EquatableArray<UnrealProperty>(arguments);
        
        string fullNamespace = namedTypeSymbol.ContainingNamespace.ToDisplayString();
        string typedArguments = string.Join(", ", InnerTypes.Select(t => t.ManagedType));
        ManagedType = $"{fullNamespace}.{namedTypeSymbol.Name}<{typedArguments}>";
    }
    
    public TemplateProperty(EquatableArray<UnrealProperty> innerTypes, PropertyType propertyType, string marshaller, string sourceName, Accessibility accessibility, string protection, UnrealType outer) 
        : base(propertyType, sourceName, accessibility, outer)
    {
        CacheNativeTypePtr = true;
        
        _marshallerName = marshaller;
        InnerTypes = innerTypes;
        SourceName = sourceName;
        
        string fullNamespace = "System.Collections.Generic";
        string typedArguments = string.Join(", ", InnerTypes.Select(t => t.ManagedType));
        ManagedType = $"{fullNamespace}.{marshaller}<{typedArguments}>";
    }
    
    public string MakeMarshallerType(string marshallerName, params string[] innerTypes)
    {
        return $"{marshallerName}<{string.Join(", ", innerTypes)}>";
    }

    public override void MakeProperty(GeneratorStringBuilder builder, string ownerPtr)
    {
        base.MakeProperty(builder, ownerPtr);
        
        string templateArrayName = $"{SourceName}_TemplateArray";
        builder.AppendLine($"IntPtr {templateArrayName} = InitTemplateProps({BuilderNativePtr}, {InnerTypes.Count});");
        
        for (int i = 0; i < InnerTypes.Count; i++)
        {
            UnrealProperty innerType = InnerTypes[i];
            innerType.MakeProperty(builder, templateArrayName);
        }
    }

    public virtual bool Equals(TemplateProperty? other)
    {
        return base.Equals(other) && InnerTypes.Equals(other.InnerTypes);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = base.GetHashCode();
            hashCode = (hashCode * 397) ^ InnerTypes.GetHashCode();
            return hashCode;
        }
    }
}
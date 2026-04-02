using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnrealSharp.GlueGenerator.NativeTypes;

namespace UnrealSharp.GlueGenerator;

public sealed class InspectorData
{
    public InspectorData(InspectAttribute inspectAttribute)
    {
        Specifiers = new List<KeyValuePair<string, InspectAttributeArgumentDelegate>>(2);
        InspectAttribute = inspectAttribute;
    }

    public readonly InspectAttribute InspectAttribute;
    public InspectAttributeDelegate? InspectAttributeDelegate;
    public readonly List<KeyValuePair<string, InspectAttributeArgumentDelegate>> Specifiers;
    
    public InspectAttributeArgumentDelegate this[string specifierName]
    {
        get
        {
            // Dictionary lookup will likely be slower than a simple loop for small collections.
            foreach (KeyValuePair<string, InspectAttributeArgumentDelegate> kvp in Specifiers)
            {
                if (kvp.Key.Equals(specifierName, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }

            throw new KeyNotFoundException($"Specifier '{specifierName}' not found.");
        }
    }

    public UnrealType ApplyInspection(UnrealType? topType, SyntaxNode? syntaxNode, GeneratorAttributeSyntaxContext ctx, ISymbol symbol, IReadOnlyList<AttributeData> attributes)
    {
        if (InspectAttributeDelegate is null)
        {
            throw new InvalidOperationException($"Inspector for attribute '{InspectAttribute.FullyQualifiedAttributeName}' has no associated inspection delegate.");
        }
        
        UnrealType newType = InspectAttributeDelegate(topType, syntaxNode, ctx, symbol, attributes);
        ApplySpecifiers(newType, attributes);
        
        newType.PostParse(symbol);
        return newType;
    }

    public void ApplySpecifiers(UnrealType topType, IReadOnlyList<AttributeData> attributes)
    {
        for (int i = 0; i < attributes.Count; i++)
        {
            AttributeData attribute = attributes[i];
            IMethodSymbol? symbol = attribute.AttributeConstructor;
            
            if (symbol is not null)
            {
                ImmutableArray<TypedConstant> constructorArguments = attribute.ConstructorArguments;
                for (int j = 0; j < constructorArguments.Length; j++)
                {
                    IParameterSymbol parameterSymbol = symbol.Parameters[j];
                    this[parameterSymbol.Name](topType, constructorArguments[j]);
                } 
            }
            
            ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments = attribute.NamedArguments;
            for (int j = 0; j < namedArguments.Length; j++)
            {
                KeyValuePair<string, TypedConstant> namedArg = namedArguments[j];
                this[namedArg.Key](topType, namedArg.Value);
            }
        }
    }

    public override string ToString()
    {
        return InspectAttribute.FullyQualifiedAttributeName;
    }
}

public delegate UnrealType InspectAttributeDelegate(UnrealType? outer, SyntaxNode? syntaxNode, GeneratorAttributeSyntaxContext ctx, ISymbol symbol, IReadOnlyList<AttributeData> attributes);
public delegate void InspectAttributeArgumentDelegate(UnrealType topType, TypedConstant constant);

public static class InspectionDispatcher
{
    public static readonly List<InspectorData> InspectorTable = new();
    
    public static InspectorData? GetInspector(string attributeName)
    {
        foreach (InspectorData inspectorData in InspectorTable)
        {
            if (inspectorData.InspectAttribute.Name.Equals(attributeName, StringComparison.OrdinalIgnoreCase))
            {
                return inspectorData;
            }
        }

        return null;
    }
    
    public static List<InspectorData> GetInspectorsForScope(string scopeName)
    {
        List<InspectorData> foundData = new();
        
        foreach (InspectorData inspectorData in InspectorTable)
        {
            if (inspectorData.InspectAttribute.Scope.Equals(scopeName, StringComparison.OrdinalIgnoreCase))
            {
                foundData.Add(inspectorData);
            }
        }

        return foundData;
    }
    
    public static bool TryGetInspectorData(string attributeName, out InspectorData? inspectorData)
    {
        InspectorData? foundData = GetInspector(attributeName);
        inspectorData = foundData;
        return foundData != null;
    }

    static InspectionDispatcher()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        foreach (Type type in assembly.GetTypes())
        {
            if (!type.IsDefined(typeof(Inspector), inherit: false))
            {
                continue;
            }

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                InspectAttribute? inspectAttribute = method.GetCustomAttribute<InspectAttribute>(inherit: false);
                if (inspectAttribute is null)
                {
                    continue;
                }
                
                InspectAttributeDelegate inspectAttributeDelegate = (InspectAttributeDelegate) Delegate.CreateDelegate(typeof(InspectAttributeDelegate), method);

                foreach (string attributeName in inspectAttribute.Names)
                {
                    if (!TryGetInspectorData(attributeName, out InspectorData? inspectorData))
                    {
                        inspectorData = new InspectorData(inspectAttribute);
                        InspectorTable.Add(inspectorData);
                    }

                    inspectorData!.InspectAttributeDelegate = inspectAttributeDelegate;
                }
            }
            
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                InspectArgumentAttribute? specifierAttr = method.GetCustomAttribute<InspectArgumentAttribute>(inherit: false);
                if (specifierAttr is null)
                {
                    continue;
                }
                
                InspectAttributeArgumentDelegate inspectAttributeArgumentDelegate = (InspectAttributeArgumentDelegate)Delegate.CreateDelegate(typeof(InspectAttributeArgumentDelegate), method);

                foreach (string attributeName in specifierAttr.AttributeNames)
                {
                    TryGetInspectorData(attributeName, out InspectorData? inspectorData);
                    
                    if (inspectorData is null)
                    {
                        throw new InvalidOperationException($"Specifier method '{method.Name}' references unknown attribute '{attributeName}'. Ensure the attribute is defined with an [Inspect] method first.");
                    }
                        
                    foreach (string specifierName in specifierAttr.SpecifierNames)
                    {
                        inspectorData!.Specifiers.Add(new KeyValuePair<string, InspectAttributeArgumentDelegate>(specifierName, inspectAttributeArgumentDelegate));
                    }
                }
            }
        }
    }

    struct InspectionContext
    {
        public InspectorData InspectorData;
        public List<AttributeData> Attributes;
    }
    
    public static void InspectMembers(UnrealType topType, ITypeSymbol typeSymbol, TypeDeclarationSyntax typeDeclaration, GeneratorAttributeSyntaxContext ctx)
    {
        bool iteratedMembersFromSyntax  = false;
        foreach (MemberDeclarationSyntax memberDecl in typeDeclaration.Members)
        {
            ImmutableArray<ISymbol> memberSymbols = GetDeclaredSymbolsForMember(memberDecl, ctx.SemanticModel);
            
            if (memberSymbols.IsDefaultOrEmpty)
            {
                continue;
            }
            
            RunMemberInspections(topType, memberDecl, memberSymbols, ctx);
            iteratedMembersFromSyntax  = true;
        }

        // Fallback: iterate all members from the symbol. Records with primary constructors don't have member declarations.
        if (!iteratedMembersFromSyntax)
        {
            RunMemberInspections(topType, null, typeSymbol.GetMembers(), ctx);
        }
    }

    private static void RunMemberInspections(UnrealType topType, MemberDeclarationSyntax? memberDecl, ImmutableArray<ISymbol> memberSymbols, GeneratorAttributeSyntaxContext ctx)
    {
        foreach (ISymbol memberSymbol in memberSymbols)
        {
            List<InspectionContext>? inspections = null;
            ImmutableArray<AttributeData> attributes = memberSymbol.GetAttributes();

            foreach (AttributeData attribute in attributes)
            {
                string attributeName = attribute.AttributeClass!.Name;
                if (!TryGetInspectorData(attributeName, out InspectorData? inspectorData))
                {
                    continue;
                }

                inspections ??= new List<InspectionContext>();

                InspectionContext? foundContext = null;
                int index = -1;
                for (int i = 0; i < inspections.Count; i++)
                {
                    if (inspections[i].InspectorData != inspectorData)
                    {
                        continue;
                    }
                    
                    foundContext = inspections[i];
                    index = i;
                    break;
                }

                if (foundContext is null)
                {
                    foundContext = new InspectionContext
                    {
                        InspectorData = inspectorData!,
                        Attributes = new List<AttributeData>()
                    };
                }

                foundContext.Value.Attributes.Add(attribute);

                if (index >= 0)
                {
                    inspections[index] = foundContext.Value;
                }
                else
                {
                    inspections.Add(foundContext.Value);
                }
            }

            if (inspections is null)
            {
                continue;
            }

            foreach (InspectionContext inspectionContext in inspections)
            {
                inspectionContext.InspectorData.ApplyInspection(topType, memberDecl, ctx, memberSymbol, inspectionContext.Attributes);
            }
        }
    }
    
    static ImmutableArray<ISymbol> GetDeclaredSymbolsForMember(MemberDeclarationSyntax memberDecl, SemanticModel semanticModel)
    {
        ISymbol? direct = semanticModel.GetDeclaredSymbol(memberDecl);
        if (direct is not null)
        {
            return ImmutableArray.Create(direct);
        }

        if (memberDecl is not FieldDeclarationSyntax fieldDecl)
        {
            return ImmutableArray<ISymbol>.Empty;
        }
        
        ImmutableArray<ISymbol>.Builder builder = ImmutableArray.CreateBuilder<ISymbol>();
        
        foreach (VariableDeclaratorSyntax variable in fieldDecl.Declaration.Variables)
        {
            ISymbol? symbol = semanticModel.GetDeclaredSymbol(variable);
            if (symbol is not null)
            {
                builder.Add(symbol);
            }
        }
        
        return builder.ToImmutable();
    }
    
    public static void InspectSpecifiers(string attributeName, UnrealType topType, IReadOnlyList<AttributeData> attributes)
    {
        if (!TryGetInspectorData(attributeName, out InspectorData? inspectorData))
        {
            return;
        }
        
        inspectorData!.ApplySpecifiers(topType, attributes);
    }
    
    public static IReadOnlyList<AttributeData> GetAttributesByName(ISymbol symbol, string attributeName)
    {
        List<AttributeData> foundAttributes = new();
        ImmutableArray<AttributeData> attributes = symbol.GetAttributes();
        
        foreach (AttributeData attribute in attributes)
        {
            if (attribute.AttributeClass!.Name.Equals(attributeName, StringComparison.OrdinalIgnoreCase))
            {
                foundAttributes.Add(attribute);
            }
        }

        return foundAttributes;
    }
}

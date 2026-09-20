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

    public bool TryGetSpecifier(string specifierName, out InspectAttributeArgumentDelegate? handler)
    {
        foreach (KeyValuePair<string, InspectAttributeArgumentDelegate> kvp in Specifiers)
        {
            if (!kvp.Key.Equals(specifierName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            handler = kvp.Value;
            return true;
        }

        handler = null;
        return false;
    }

    public UnrealType ApplyInspection(UnrealType? topType, SyntaxNode? syntaxNode, GeneratorAttributeSyntaxContext ctx,
        ISymbol symbol, IReadOnlyList<AttributeData> attributes)
    {
        if (InspectAttributeDelegate is null)
        {
            throw new InvalidOperationException(
                $"Inspector for attribute '{InspectAttribute.FullyQualifiedAttributeName}' has no associated inspection delegate.");
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
            IMethodSymbol? constructor = attribute.AttributeConstructor;

            if (constructor is not null)
            {
                ImmutableArray<TypedConstant> constructorArguments = attribute.ConstructorArguments;

                for (int j = 0; j < constructorArguments.Length; j++)
                {
                    if (j >= constructor.Parameters.Length)
                    {
                        break;
                    }

                    IParameterSymbol parameterSymbol = constructor.Parameters[j];

                    if (!TryGetSpecifier(parameterSymbol.Name, out InspectAttributeArgumentDelegate? handler))
                    {
                        continue;
                    }

                    handler!(topType, constructorArguments[j]);
                }
            }

            ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments = attribute.NamedArguments;

            for (int j = 0; j < namedArguments.Length; j++)
            {
                KeyValuePair<string, TypedConstant> namedArg = namedArguments[j];

                if (!TryGetSpecifier(namedArg.Key, out InspectAttributeArgumentDelegate? handler))
                {
                    continue;
                }

                handler!(topType, namedArg.Value);
            }
        }
    }

    public override string ToString()
    {
        return InspectAttribute.FullyQualifiedAttributeName;
    }
}

public delegate UnrealType InspectAttributeDelegate(UnrealType? outer, SyntaxNode? syntaxNode,
    GeneratorAttributeSyntaxContext ctx, ISymbol symbol, IReadOnlyList<AttributeData> attributes);

public delegate void InspectAttributeArgumentDelegate(UnrealType topType, TypedConstant constant);

public static class InspectionDispatcher
{
    private static readonly Dictionary<string, InspectorData> InspectorsByName;
    private static readonly List<InspectorData> AllInspectors;
    public static readonly string? InitializationError;

    public static IReadOnlyList<InspectorData> InspectorTable => AllInspectors;

    public static InspectorData? GetInspector(string attributeName)
    {
        return InspectorsByName.TryGetValue(attributeName, out InspectorData? data) ? data : null;
    }

    public static List<InspectorData> GetInspectorsForScope(string scopeName)
    {
        List<InspectorData> foundData = new List<InspectorData>();

        foreach (InspectorData inspectorData in AllInspectors)
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
        return InspectorsByName.TryGetValue(attributeName, out inspectorData);
    }

    static InspectionDispatcher()
    {
        InspectorsByName = new Dictionary<string, InspectorData>(StringComparer.OrdinalIgnoreCase);
        AllInspectors = new List<InspectorData>();

        try
        {
            BuildInspectorTable();
        }
        catch (Exception exception)
        {
            InitializationError = exception.ToString();
        }
    }

    private static void BuildInspectorTable()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        Type[] types;

        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException loadException)
        {
            List<Type> loaded = new List<Type>();

            foreach (Type? type in loadException.Types)
            {
                if (type != null)
                {
                    loaded.Add(type);
                }
            }

            types = loaded.ToArray();
        }

        const BindingFlags methodFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                         BindingFlags.DeclaredOnly;
        List<(Type Type, MethodInfo[] Methods)> reflected = new List<(Type, MethodInfo[])>();

        foreach (Type type in types)
        {
            if (!type.IsDefined(typeof(Inspector), inherit: false))
            {
                continue;
            }

            reflected.Add((type, type.GetMethods(methodFlags)));
        }

        foreach ((Type _, MethodInfo[] methods) in reflected)
        {
            foreach (MethodInfo method in methods)
            {
                InspectAttribute? inspectAttribute = method.GetCustomAttribute<InspectAttribute>(inherit: false);

                if (inspectAttribute is null)
                {
                    continue;
                }

                InspectAttributeDelegate inspectAttributeDelegate =
                    (InspectAttributeDelegate)Delegate.CreateDelegate(typeof(InspectAttributeDelegate), method);
                string[] attributeNames = inspectAttribute.Names;

                InspectorData? inspectorData = null;

                foreach (string attributeName in attributeNames)
                {
                    if (!string.IsNullOrEmpty(attributeName) &&
                        InspectorsByName.TryGetValue(attributeName, out InspectorData? existing))
                    {
                        inspectorData = existing;
                        break;
                    }
                }

                if (inspectorData is null)
                {
                    inspectorData = new InspectorData(inspectAttribute);
                    AllInspectors.Add(inspectorData);
                }
                else if (inspectorData.InspectAttributeDelegate != null &&
                         inspectorData.InspectAttributeDelegate != inspectAttributeDelegate)
                {
                    throw new InvalidOperationException(
                        $"Attribute '{attributeNames[0]}' is claimed by more than one [Inspect] method " +
                        $"('{method.DeclaringType?.Name}.{method.Name}' and an earlier one). Each attribute needs exactly one.");
                }

                inspectorData.InspectAttributeDelegate = inspectAttributeDelegate;

                foreach (string attributeName in attributeNames)
                {
                    if (string.IsNullOrEmpty(attributeName))
                    {
                        continue;
                    }

                    InspectorsByName[attributeName] = inspectorData;
                }
            }
        }

        foreach ((Type _, MethodInfo[] methods) in reflected)
        {
            foreach (MethodInfo method in methods)
            {
                InspectArgumentAttribute? specifierAttr =
                    method.GetCustomAttribute<InspectArgumentAttribute>(inherit: false);

                if (specifierAttr is null)
                {
                    continue;
                }

                InspectAttributeArgumentDelegate inspectAttributeArgumentDelegate =
                    (InspectAttributeArgumentDelegate)Delegate.CreateDelegate(typeof(InspectAttributeArgumentDelegate),
                        method);

                foreach (string attributeName in specifierAttr.AttributeNames)
                {
                    if (!InspectorsByName.TryGetValue(attributeName, out InspectorData? inspectorData))
                    {
                        throw new InvalidOperationException(
                            $"Specifier method '{method.Name}' references unknown attribute '{attributeName}'. Ensure the attribute is defined with an [Inspect] method first.");
                    }

                    foreach (string specifierName in specifierAttr.SpecifierNames)
                    {
                        inspectorData.Specifiers.Add(
                            new KeyValuePair<string, InspectAttributeArgumentDelegate>(specifierName,
                                inspectAttributeArgumentDelegate));
                    }
                }
            }
        }
    }

    private sealed class InspectionContext
    {
        public InspectionContext(InspectorData inspectorData)
        {
            InspectorData = inspectorData;
            Attributes = new List<AttributeData>();
        }

        public readonly InspectorData InspectorData;
        public readonly List<AttributeData> Attributes;
    }

    public static void InspectMembers(UnrealType topType, ITypeSymbol typeSymbol, TypeDeclarationSyntax typeDeclaration,
        GeneratorAttributeSyntaxContext ctx)
    {
        if (typeDeclaration.ParameterList != null)
        {
            RunMemberInspections(topType, null, typeSymbol.GetMembers(), ctx);
        }
        else
        {
            foreach (MemberDeclarationSyntax memberDeclaration in typeDeclaration.Members)
            {
                ImmutableArray<ISymbol> memberSymbols =
                    GetDeclaredSymbolsForMember(memberDeclaration, ctx.SemanticModel);

                if (memberSymbols.IsDefaultOrEmpty)
                {
                    continue;
                }

                RunMemberInspections(topType, memberDeclaration, memberSymbols, ctx);
            }
        }
    }

    private static void RunMemberInspections(UnrealType topType, MemberDeclarationSyntax? memberDecl,
        ImmutableArray<ISymbol> memberSymbols, GeneratorAttributeSyntaxContext ctx)
    {
        foreach (ISymbol memberSymbol in memberSymbols)
        {
            List<InspectionContext>? inspections = null;
            ImmutableArray<AttributeData> attributes = memberSymbol.GetAttributes();

            foreach (AttributeData attribute in attributes)
            {
                string? attributeName = attribute.AttributeClass?.Name;

                if (attributeName is null || !TryGetInspectorData(attributeName, out InspectorData? inspectorData))
                {
                    continue;
                }

                inspections ??= new List<InspectionContext>();

                InspectionContext? foundContext = null;

                foreach (InspectionContext existing in inspections)
                {
                    if (existing.InspectorData == inspectorData)
                    {
                        foundContext = existing;
                        break;
                    }
                }

                if (foundContext is null)
                {
                    foundContext = new InspectionContext(inspectorData!);
                    inspections.Add(foundContext);
                }

                foundContext.Attributes.Add(attribute);
            }

            if (inspections is null)
            {
                continue;
            }

            foreach (InspectionContext inspectionContext in inspections)
            {
                inspectionContext.InspectorData.ApplyInspection(topType, memberDecl, ctx, memberSymbol,
                    inspectionContext.Attributes);
            }
        }
    }

    private static ImmutableArray<ISymbol> GetDeclaredSymbolsForMember(MemberDeclarationSyntax memberDecl,
        SemanticModel semanticModel)
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

    public static void InspectSpecifiers(string attributeName, UnrealType topType,
        IReadOnlyList<AttributeData> attributes)
    {
        if (!TryGetInspectorData(attributeName, out InspectorData? inspectorData))
        {
            return;
        }

        inspectorData!.ApplySpecifiers(topType, attributes);
    }
}
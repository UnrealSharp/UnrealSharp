using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes;

[Flags]
public enum EClassFlags : ulong
{
    None				  = 0x00000000u,
    Abstract            = 0x00000001u,
    DefaultConfig		  = 0x00000002u,
    Config			  = 0x00000004u,
    Transient			  = 0x00000008u,
    Optional            = 0x00000010u,
    MatchedSerializers  = 0x00000020u,
    ProjectUserConfig	  = 0x00000040u,
    Native			  = 0x00000080u,
    NoExport = 0x00000100u,
    NotPlaceable        = 0x00000200u,
    PerObjectConfig     = 0x00000400u,
    ReplicationDataIsSetUp = 0x00000800u,
    EditInlineNew		  = 0x00001000u,
    CollapseCategories  = 0x00002000u,
    Interface           = 0x00004000u,
    CustomConstructor = 0x00008000u,
    Const			      = 0x00010000u,
    NeedsDeferredDependencyLoading = 0x00020000u,
    CompiledFromBlueprint  = 0x00040000u,
    MinimalAPI	      = 0x00080000u,
    RequiredAPI	      = 0x00100000u,
    DefaultToInstanced  = 0x00200000u,
    TokenStreamAssembled  = 0x00400000u,
    HasInstancedReference= 0x00800000u,
    Hidden			  = 0x01000000u,
    Deprecated		  = 0x02000000u,
    HideDropDown		  = 0x04000000u,
    GlobalUserConfig	  = 0x08000000u,
    Intrinsic			  = 0x10000000u,
    Constructed		  = 0x20000000u,
    ConfigDoNotCheckDefaults = 0x40000000u,
    NewerVersionExists  = 0x80000000u,
}

[Inspector]
public record UnrealClass : UnrealClassBase
{
    public override FieldType FieldType => FieldType.Class;
    
    const string UClassAttributeName = "UClassAttribute";
    const string LongUClassAttributeName = "UnrealSharp.Attributes.UClassAttribute";
    
    public string Config = string.Empty;
    
    public FieldName ParentClass;

    public EquatableList<string> Overrides;
    public EquatableList<FieldName> Interfaces;

    public EquatableList<ComponentOverride> ComponentOverrides;
    
    public string FullParentName => string.IsNullOrEmpty(ParentClass.Namespace) ? ParentClass.Name : $"{ParentClass.Namespace}.{ParentClass.Name}";
    
    public UnrealClass(ITypeSymbol typeSymbol, UnrealType? outer = null) : base(typeSymbol, outer)
    {
        if (typeSymbol.BaseType == null)
        {
            throw new InvalidOperationException($"Type {typeSymbol.Name} does not have a base type. Needs to inherit from UObject class.");
        }

        if (!typeSymbol.BaseType.IsChildOf("UObject"))
        {
            throw new InvalidOperationException($"'{typeSymbol.Name}' inherits from '{typeSymbol.BaseType.Name}' which does not inherit from 'UObject'. All UClass types must ultimately inherit from UObject.");
        }

        ParentClass = new FieldName(typeSymbol.BaseType!);
        
        ImmutableArray<INamedTypeSymbol> immutableArray = typeSymbol.Interfaces;
        
        if (immutableArray.Length > 0)
        {
            EquatableList<FieldName> interfaces = new EquatableList<FieldName>(new List<FieldName>(immutableArray.Length - 1));

            foreach (INamedTypeSymbol baseType in immutableArray)
            {
                if (baseType is null || baseType.TypeKind != TypeKind.Interface || !baseType.HasAttribute("UInterfaceAttribute"))
                {
                    continue;
                }
                
                FieldName interfaceData = new FieldName(baseType);
                
                interfaces.List.Add(interfaceData);
                
                ImmutableArray<ISymbol> members = baseType.GetMembers();
                Functions.List.Capacity += members.Length;
            
                foreach (ISymbol member in members)
                {
                    if (member.Kind != SymbolKind.Method || !member.HasUFunctionAttribute())
                    {
                        continue;
                    }
                    
                    UnrealFunction function = new UnrealFunction((IMethodSymbol) member, this);
                    function.TypeAccessibility = function.TypeAccessibility == Accessibility.NotApplicable ? Accessibility.Public : function.TypeAccessibility;
                
                    List<AttributeData> attributes = member.GetAttributesByName("UFunctionAttribute");
                    InspectorManager.InspectSpecifiers("UFunctionAttribute", function, attributes);
                
                    Functions.List.Add(function);
                }
                
                Interfaces = interfaces;
            }
        }
        
        Overrides = new EquatableList<string>(new List<string>());
        
        ImmutableArray<ISymbol> classMembers = typeSymbol.GetMembers();
        foreach (ISymbol member in classMembers)
        {
            if (member.Kind != SymbolKind.Method)
            {
                continue;
            }
            
            if (!member.IsOverride)
            {
                continue;
            }
            
            IMethodSymbol methodSymbol = (IMethodSymbol) member;
            
            while (true)
            {
                IMethodSymbol? originalMethodSymbol = methodSymbol.OverriddenMethod;
                
                if (originalMethodSymbol == null)
                {
                    break;
                }
                
                methodSymbol = originalMethodSymbol;
            }
            
            if (!methodSymbol.HasUFunctionAttribute() && !methodSymbol.Name.EndsWith("_Implementation"))
            {
                continue;
            }

            string nativeName = methodSymbol.TryGetEngineName();
            
            if (!string.IsNullOrEmpty(nativeName))
            {
                Overrides.List.Add(nativeName);
            }
        }
        
        List<AttributeData> overrideComponentAttribute = typeSymbol.GetAttributesByName("OverrideComponentAttribute");
        ComponentOverrides = new EquatableList<ComponentOverride>(new List<ComponentOverride>(overrideComponentAttribute.Count));
        
        foreach (AttributeData attributeData in overrideComponentAttribute)
        {
            INamedTypeSymbol? componentType = attributeData.TryGetAttributeConstructorArgument<INamedTypeSymbol>(0);
            string? overrideWithName = attributeData.TryGetAttributeConstructorArgument<string>(1);
            string? optionalPropertyName = attributeData.TryGetAttributeConstructorArgument<string>(2);
            
            if (componentType == null || string.IsNullOrEmpty(overrideWithName))
            {
                continue;
            }
            
            ISymbol? symbol = typeSymbol.BaseType!.GetMemberSymbolByName(overrideWithName!);
            if (symbol == null || symbol.Kind != SymbolKind.Property)
            {
                continue;
            }
            
            IPropertySymbol propertySymbol = (IPropertySymbol) symbol;
            INamedTypeSymbol propertyType = (INamedTypeSymbol) propertySymbol.Type;
            
            if (!componentType.IsChildOf(propertyType))
            {
                continue;
            }
            
            ComponentOverride componentOverride = new ComponentOverride(symbol.ContainingType, componentType, overrideWithName!, symbol.DeclaredAccessibility, optionalPropertyName);
            ComponentOverrides.List.Add(componentOverride);
        }
    }
    
    public UnrealClass(EClassFlags flags, string parentName, string parentNamespace, string sourceName, string typeNameSpace, Accessibility accessibility, string assemblyName, UnrealType? outer = null) 
        : base(parentName, parentNamespace, sourceName, typeNameSpace, accessibility, assemblyName, outer)
    {
        ParentClass = new FieldName(parentName, parentNamespace, assemblyName);
        ClassFlags = flags;
    }

    [Inspect(LongUClassAttributeName, UClassAttributeName, "Global")]
    public static UnrealType? UClassAttribute(UnrealType? outer, SyntaxNode? syntaxNode, GeneratorAttributeSyntaxContext ctx, ISymbol symbol, IReadOnlyList<AttributeData> attributes)
    {
        ITypeSymbol typeSymbol = (ITypeSymbol) symbol;
        UnrealClass unrealClass = new UnrealClass(typeSymbol);
        
        InspectorManager.InspectSpecifiers(UClassAttributeName, unrealClass, attributes);
        InspectorManager.InspectTypeMembers(unrealClass, ctx.TargetNode, typeSymbol, ctx);
        
        return unrealClass;
    }
    
    [InspectArgument(["ClassFlags", "flags"], UClassAttributeName)]
    public static void ClassFlagsSpecifier(UnrealType topScope, TypedConstant constant)
    {
        UnrealClass unrealClass = (UnrealClass) topScope;
        unrealClass.ClassFlags |= (EClassFlags) constant.Value!;
        
        if (unrealClass.ClassFlags.HasFlag(EClassFlags.Config) && 
            !unrealClass.ClassFlags.HasFlag(EClassFlags.GlobalUserConfig | EClassFlags.DefaultConfig | EClassFlags.ProjectUserConfig))
        {
            unrealClass.ClassFlags |= EClassFlags.DefaultConfig;
        }
    }

    [InspectArgument(["Config", "config"], UClassAttributeName)]
    public static void ConfigCategorySpecifier(UnrealType topScope, TypedConstant constant)
    {
        UnrealClass unrealClass = (UnrealClass)topScope;
        unrealClass.Config = (string) constant.Value!;
    }

    public override void ExportType(GeneratorStringBuilder builder, SourceProductionContext spc)
    {
        builder.BeginType(this, SourceGenUtilities.ClassKeyword);
            
        builder.BeginTypeStaticConstructor(this);
        ExportBackingVariablesToStaticConstructor(builder, SourceGenUtilities.NativeTypePtr);
        builder.EndTypeStaticConstructor();
        
        ExportList(builder, spc, Properties);
        ExportComponentOverrides(builder);
        
        ExportList(builder, spc, Functions);
        ExportList(builder, spc, AsyncFunctions);
        
        builder.CloseBrace();
    }
    
    public override void ExportBackingVariablesToStaticConstructor(GeneratorStringBuilder builder, string nativeType)
    {
        base.ExportBackingVariablesToStaticConstructor(builder, nativeType);
        Functions.ExportListToStaticConstructor(builder, nativeType);
        AsyncFunctions.ExportListToStaticConstructor(builder, nativeType);
    }
    
    public void ExportComponentOverrides(GeneratorStringBuilder builder)
    {
        if (ComponentOverrides.Count == 0)
        {
            return;
        }
        
        foreach (ComponentOverride componentOverride in ComponentOverrides.List)
        {
            if (string.IsNullOrEmpty(componentOverride.OptionalPropertyName))
            {
                continue;
            }
            
            string accessibilityPrefix = componentOverride.Accessibility.AccessibilityToString();
            builder.AppendLine($"{accessibilityPrefix}{componentOverride.OverrideComponentType.FullName} {componentOverride.OptionalPropertyName} => ({componentOverride.OverrideComponentType.FullName}){componentOverride.OverridePropertyName};");
        }
        
        builder.AppendLine();
    }

    public void ExportList<T>(GeneratorStringBuilder builder, SourceProductionContext spc, EquatableList<T> list) where T : UnrealType, IEquatable<T>
    {
        if (list.Count == 0)
        {
            return;
        }
        
        foreach (T item in list.List)
        {
            item.ExportType(builder, spc);
        }
        
        builder.AppendLine();
    }
    
    public override void PopulateJsonObject(JsonObject jsonObject)
    {
        base.PopulateJsonObject(jsonObject);
        
        ParentClass.SerializeToJson(jsonObject, "ParentClass", true);
        
        jsonObject.TrySetJsonEnum("ClassFlags", ClassFlags);
        jsonObject.TrySetJsonString("Config", Config);
        
        Overrides.PopulateJsonWithArray(jsonObject, "Overrides", array =>
        {
            foreach (string overrideName in Overrides.List)
            {
                array.Add(overrideName);
            }
        });
        
        Interfaces.PopulateJsonWithArray(jsonObject, "Interfaces", array =>
        {
            foreach (FieldName interfaceName in Interfaces.List)
            {
                JsonObject interfaceObject = interfaceName.SerializeToJson(true)!;
                array.Add(interfaceObject);
            }
        });
        
        ComponentOverrides.PopulateJsonWithArray(jsonObject, "ComponentOverrides", array =>
        {
            foreach (ComponentOverride componentOverride in ComponentOverrides.List)
            {
                JsonObject componentObject = new JsonObject
                {
                    ["OwningClass"] = componentOverride.OwningClass.SerializeToJson(true),
                    ["ComponentType"] = componentOverride.OverrideComponentType.SerializeToJson(true),
                    ["PropertyName"] = componentOverride.OverridePropertyName,
                };
                
                array.Add(componentObject);
            }
        });
    }
}
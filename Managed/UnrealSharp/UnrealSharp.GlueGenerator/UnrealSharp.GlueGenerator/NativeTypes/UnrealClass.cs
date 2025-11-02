using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnrealSharp.GlueGenerator.NativeTypes.Properties;

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

public record struct InterfaceData
{
    public string Name;
    public string FullName;
}

[Inspector]
public record UnrealClass : UnrealClassBase
{
    const string UClassAttributeName = "UClassAttribute";
    const string LongUClassAttributeName = "UnrealSharp.Attributes.UClassAttribute";

    public override int FieldTypeValue => 0;
    
    public string ConfigCategory = string.Empty;

    public EquatableList<string> Overrides;
    public EquatableList<InterfaceData> Interfaces;

    public string ParameterlessCtorBodyHash;
    
    public UnrealClass(SemanticModel model, ITypeSymbol typeSymbol, SyntaxNode syntax, UnrealType? outer = null) : base(typeSymbol, syntax, outer)
    {
        ClassDeclarationSyntax declarationSyntax = (ClassDeclarationSyntax) syntax;
        BaseListSyntax baseList = declarationSyntax.BaseList!;

        if (baseList.Types.Count > 1)
        {
            EquatableList<InterfaceData> interfaces = new EquatableList<InterfaceData>(new List<InterfaceData>(baseList.Types.Count - 1));

            foreach (BaseTypeSyntax baseType in baseList.Types)
            {
                ITypeSymbol? baseTypeSymbol = ModelExtensions.GetTypeInfo(model, baseType.Type).Type;
                if (baseTypeSymbol is null || baseTypeSymbol.TypeKind != TypeKind.Interface || !baseTypeSymbol.HasAttribute("UInterfaceAttribute"))
                {
                    continue;
                }
                
                InterfaceData interfaceData = new InterfaceData
                {
                    Name = baseTypeSymbol.Name,
                    FullName = baseTypeSymbol.ToDisplayString()
                };
                
                interfaces.List.Add(interfaceData);
                
                ImmutableArray<ISymbol> members = baseTypeSymbol.GetMembers();
                Functions.List.Capacity += members.Length;
            
                foreach (ISymbol member in members)
                {
                    if (member.Kind != SymbolKind.Method || !member.HasUFunctionAttribute())
                    {
                        continue;
                    }
                    
                    MethodDeclarationSyntax methodDeclaration = (MethodDeclarationSyntax) member.DeclaringSyntaxReferences[0].GetSyntax();
                    SemanticModel methodModel = model.Compilation.GetSemanticModel(methodDeclaration.SyntaxTree);
                    UnrealFunction function = new UnrealFunction(methodModel, member, methodDeclaration, this);
                    function.Protection = function.Protection == Accessibility.NotApplicable ? Accessibility.Public : function.Protection;
                
                    List<AttributeData> attributes = member.GetAttributesByName("UFunctionAttribute");
                    InspectorManager.InspectSpecifiers("UFunctionAttribute", function, attributes);
                
                    Functions.List.Add(function);
                }
                
                Interfaces = interfaces;
            }
        }
        
        Overrides = new EquatableList<string>(new List<string>());
        
        foreach (MemberDeclarationSyntax member in declarationSyntax.Members)
        {
            if (member.RawKind != (int) SyntaxKind.MethodDeclaration)
            {
                continue;
            }
            
            ISymbol symbol = ModelExtensions.GetDeclaredSymbol(model, member)!;
            
            if (!symbol.IsOverride || !symbol.Name.EndsWith("_Implementation"))
            {
                continue;
            }
            
            IMethodSymbol methodSymbol = (IMethodSymbol) symbol;
            
            while (true)
            {
                IMethodSymbol? originalMethodSymbol = methodSymbol.OverriddenMethod;
                
                if (originalMethodSymbol == null)
                {
                    break;
                }
                
                methodSymbol = originalMethodSymbol;
            }

            string nativeName = methodSymbol.TryGetEngineName();
            
            if (!string.IsNullOrEmpty(nativeName))
            {
                Overrides.List.Add(nativeName);
            }
        }
        
        // TODO: TEMP: I want to move this to CompilationManager.DirtyFile
        {
            ClassDeclarationSyntax classDeclaration = (ClassDeclarationSyntax)syntax;
            ConstructorDeclarationSyntax? defaultConstructor = classDeclaration.Members
                .OfType<ConstructorDeclarationSyntax>()
                .FirstOrDefault(c =>
                    c.ParameterList is { Parameters.Count: 0 } &&
                    !c.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)));

            ParameterlessCtorBodyHash = ProcessConstructor(defaultConstructor);
        }
    }
    
    private static string ProcessConstructor(ConstructorDeclarationSyntax? ctor)
    {
        if (ctor is null)
        {
            return String.Empty;
        }

        SyntaxNode? bodyNode = null;
        
        if (ctor.Body is not null)
        {
            bodyNode = ctor.Body;
        }
        else if (ctor.ExpressionBody is not null)
        {
            bodyNode = ctor.ExpressionBody;
        }
        
        if (bodyNode is null)
        {
            return String.Empty;
        }

        StringBuilder stringBuilder = new StringBuilder(256);
        
        IEnumerable<SyntaxToken> tokens = bodyNode.DescendantTokens();
        foreach (SyntaxToken token in tokens)
        {
            object? tokenValue = token.Value;
            
            if (tokenValue is null)
            {
                continue;
            }
            
            stringBuilder.Append(token.Value);
        }

        using SHA256? sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(stringBuilder.ToString())));
    }
    
    public UnrealClass(EClassFlags flags, string parentName, string parentNamespace, string sourceName, string typeNameSpace, Accessibility accessibility, string assemblyName, UnrealType? outer = null) 
        : base(parentName, parentNamespace, sourceName, typeNameSpace, accessibility, assemblyName, outer)
    {
        ClassFlags = flags;
    }

    [Inspect(LongUClassAttributeName, UClassAttributeName, "Global")]
    public static UnrealType? UClassAttribute(UnrealType? outer, GeneratorAttributeSyntaxContext ctx, MemberDeclarationSyntax declarationSyntax, IReadOnlyList<AttributeData> attributes)
    {
        ITypeSymbol typeSymbol = (ITypeSymbol) ModelExtensions.GetDeclaredSymbol(ctx.SemanticModel, declarationSyntax)!;
        UnrealClass unrealClass = new UnrealClass(ctx.SemanticModel, typeSymbol, ctx.TargetNode);
        InspectorManager.InspectSpecifiers(UClassAttributeName, unrealClass, attributes);
        InspectorManager.InspectTypeMembers(unrealClass, declarationSyntax, ctx);
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

    [InspectArgument(["ConfigCategory", "config"], UClassAttributeName)]
    public static void ConfigCategorySpecifier(UnrealType topScope, TypedConstant constant)
    {
        UnrealClass unrealClass = (UnrealClass)topScope;
        unrealClass.ConfigCategory = (string) constant.Value!;
    }

    public override void ExportType(GeneratorStringBuilder builder, SourceProductionContext spc)
    {
        builder.BeginType(this, TypeKind.Class);
        
        TryExportProperties(builder, spc);
        TryExportList(builder, spc, Functions);
        TryExportList(builder, spc, AsyncFunctions);
        
        builder.CloseBrace();
    }
    
    public void TryExportList<T>(GeneratorStringBuilder builder, SourceProductionContext spc, EquatableList<T> list) where T : UnrealType, IEquatable<T>
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
    
    public void TryExportProperties(GeneratorStringBuilder builder, SourceProductionContext spc)
    {
        int numProperties = Properties.Count;
        if (numProperties == 0)
        {
            return;
        }
        
        for (int i = 0; i < numProperties; i++)
        {
            UnrealProperty property = Properties.List[i];
            property.ExportBackingVariables(builder, SourceGenUtilities.NativeTypePtr);
        }
            
        TryExportList(builder, spc, Properties);
    }
    
    public override void CreateTypeBuilder(GeneratorStringBuilder builder)
    {
        base.CreateTypeBuilder(builder);

        AppendProperties(builder, Properties.List);
        
        ulong classFlags = (ulong) ClassFlags;
        builder.AppendLine($"ModifyClass({BuilderNativePtr}, \"{ParentName.Substring(1)}\", typeof({FullParentName}), \"{ConfigCategory}\", {classFlags});");
        
        int numOverrides = Overrides.Count;
        if (numOverrides > 0)
        {
            builder.AppendLine($"InitOverrides({BuilderNativePtr}, {numOverrides});");
            
            for (int i = 0; i < numOverrides; i++)
            {
                builder.AppendLine($"NewOverride({BuilderNativePtr}, \"{Overrides.List[i]}\");");
            }
        }
        
        int numInterfaces = Interfaces.Count;
        if (numInterfaces > 0)
        {
            builder.AppendLine($"InitInterfaces({BuilderNativePtr}, {numInterfaces});");
            
            for (int i = 0; i < numInterfaces; i++)
            {
                InterfaceData interfaceData = Interfaces.List[i];
                builder.AppendLine($"NewInterface({BuilderNativePtr}, \"{interfaceData.Name.Substring(1)}\", typeof({interfaceData.FullName}));");
            }
        }
    }
}
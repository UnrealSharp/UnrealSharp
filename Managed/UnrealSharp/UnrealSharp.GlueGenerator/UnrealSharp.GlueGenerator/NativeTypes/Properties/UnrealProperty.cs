﻿using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

[Flags]
public enum EPropertyFlags : ulong
{
    None = 0,
    Edit = 0x0000000000000001,
    ConstParm = 0x0000000000000002,
    BlueprintVisible = 0x0000000000000004,
    ExportObject = 0x0000000000000008,
    BlueprintReadOnly = 0x0000000000000010,
    Net = 0x0000000000000020,
    EditFixedSize = 0x0000000000000040,
    Parm = 0x0000000000000080,
    OutParm = 0x0000000000000100,
    ZeroConstructor = 0x0000000000000200,
    ReturnParm = 0x0000000000000400,
    DisableEditOnTemplate = 0x0000000000000800,
    Transient = 0x0000000000002000,
    Config = 0x0000000000004000,
    DisableEditOnInstance = 0x0000000000010000,
    EditConst = 0x0000000000020000,
    GlobalConfig = 0x0000000000040000,
    InstancedReference = 0x0000000000080000,
    DuplicateTransient = 0x0000000000200000,
    SubobjectReference = 0x0000000000400000,
    SaveGame = 0x0000000001000000,
    NoClear = 0x0000000002000000,
    ReferenceParm = 0x0000000008000000,
    BlueprintAssignable = 0x0000000010000000,
    Deprecated = 0x0000000020000000,
    IsPlainOldData = 0x0000000040000000,
    RepSkip = 0x0000000080000000,
    RepNotify = 0x0000000100000000,
    Interp = 0x0000000200000000,
    NonTransactional = 0x0000000400000000,
    EditorOnly = 0x0000000800000000,
    NoDestructor = 0x0000001000000000,
    AutoWeak = 0x0000004000000000,
    ContainsInstancedReference = 0x0000008000000000,
    AssetRegistrySearchable = 0x0000010000000000,
    SimpleDisplay = 0x0000020000000000,
    AdvancedDisplay = 0x0000040000000000,
    Protected = 0x0000080000000000,
    BlueprintCallable = 0x0000100000000000,
    BlueprintAuthorityOnly = 0x0000200000000000,
    TextExportTransient = 0x0000400000000000,
    NonPIEDuplicateTransient = 0x0000800000000000,
    ExposeOnSpawn = 0x0001000000000000,
    PersistentInstance = 0x0002000000000000,
    UObjectWrapper = 0x0004000000000000,
    HasGetValueTypeHash = 0x0008000000000000,
    NativeAccessSpecifierPublic = 0x0010000000000000,
    NativeAccessSpecifierProtected = 0x0020000000000000,
    NativeAccessSpecifierPrivate = 0x0040000000000000,
    SkipSerialization = 0x0080000000000000,

    /* Combination flags */

    NativeAccessSpecifiers = NativeAccessSpecifierPublic | NativeAccessSpecifierProtected | NativeAccessSpecifierPrivate,

    ParmFlags = Parm | OutParm | ReturnParm | ReferenceParm | ConstParm,
    PropagateToArrayInner = ExportObject | PersistentInstance | InstancedReference | ContainsInstancedReference | Config | EditConst | Deprecated | EditorOnly | AutoWeak | UObjectWrapper,
    PropagateToMapValue = ExportObject | PersistentInstance | InstancedReference | ContainsInstancedReference | Config | EditConst | Deprecated | EditorOnly | AutoWeak | UObjectWrapper | Edit,
    PropagateToMapKey = ExportObject | PersistentInstance | InstancedReference | ContainsInstancedReference | Config | EditConst | Deprecated | EditorOnly | AutoWeak | UObjectWrapper | Edit,
    PropagateToSetElement = ExportObject | PersistentInstance | InstancedReference | ContainsInstancedReference | Config | EditConst | Deprecated | EditorOnly | AutoWeak | UObjectWrapper | Edit,

    /** the flags that should never be set on interface properties */
    InterfaceClearMask = ExportObject | InstancedReference | ContainsInstancedReference,

    /** all the properties that can be stripped for final release console builds */
    DevelopmentAssets = EditorOnly,

    /** all the properties that should never be loaded or saved */
    ComputedFlags = IsPlainOldData | NoDestructor | ZeroConstructor | HasGetValueTypeHash,

    EditDefaultsOnly = Edit | DisableEditOnInstance,
    EditInstanceOnly = Edit | DisableEditOnTemplate,
    EditAnywhere = Edit,
    
    VisibleAnywhere = BlueprintVisible | BlueprintReadOnly,
    VisibleDefaultsOnly = BlueprintVisible | BlueprintReadOnly | DisableEditOnInstance,
    VisibleInstanceOnly = BlueprintVisible | BlueprintReadOnly | DisableEditOnTemplate,
    
    BlueprintReadWrite = BlueprintVisible | Edit,

    AllFlags = 0xFFFFFFFFFFFFFFFF
}

public enum ELifetimeCondition
{
    None = 0,
    InitialOnly = 1,
    OwnerOnly = 2,	
    SkipOwner = 3,	
    SimulatedOnly = 4,	
    AutonomousOnly = 5,
    SimulatedOrPhysics = 6,
    InitialOrOwner = 7,
    Custom = 8,		
    ReplayOrOwner = 9,
    ReplayOnly = 10,		
    SimulatedOnlyNoReplay = 11,	
    SimulatedOrPhysicsNoReplay = 12,
    SkipReplay = 13,
    Dynamic = 14,				
    Never = 15,
};

[Inspector]
public record UnrealProperty : UnrealType
{
    protected const string UPropertyAttributeName = "UPropertyAttribute";
    protected const EPropertyFlags InstancedFlags = EPropertyFlags.InstancedReference | EPropertyFlags.ExportObject;

    public EPropertyFlags PropertyFlags = EPropertyFlags.None;
    public bool DefaultComponent;
    public bool RootComponent;
    public string AttachmentComponent = string.Empty;
    public string AttachmentSocket = string.Empty;
    public string ReplicatedUsing = string.Empty;
    public ELifetimeCondition LifetimeCondition = ELifetimeCondition.None;
    public string BlueprintSetter = string.Empty;
    public string BlueprintGetter = string.Empty;
    public int ArrayDim = 1;

    public readonly bool IsPartial = true;
    
    public bool IsBlittable = false;
    public RefKind RefKind;

    public readonly bool IsNullable;

    public bool CacheNativeTypePtr = false;
    
    public PropertyType PropertyType = PropertyType.Unknown;

    public string ManagedType = "";
    
    public virtual string MarshallerType => throw new NotImplementedException();

    public bool NeedsBackingFields = false;
    public bool CanInstanceMarshallerBeStatic = false;
    
    public string OffsetVariable => $"{Outer!.SourceName}_{SourceName}_Offset";
    public string NativePropertyVariable => $"{Outer!.SourceName}_{SourceName}_Property";
    public string InstancedMarshallerVariable => $"{Outer!.SourceName}_{SourceName}_Marshaller";
    
    public string CallToNative => MarshallerType + ToNative;
    public string CallFromNative => MarshallerType + FromNative;
    
    public string GetParameterDeclaration() => $"{RefKindToString()}{ManagedType}{(IsNullable ? "?" : string.Empty)} {SourceName}";
    public string GetParameterCall() => $"{RefKindToString()}{SourceName}";

    private string ToNative => ".ToNative";
    protected string FromNative => ".FromNative";

    public UnrealProperty(SyntaxNode syntaxNode, ISymbol memberSymbol, ITypeSymbol? typeSymbol, PropertyType propertyType, UnrealType? outer = null) 
        : base(memberSymbol, syntaxNode, outer)
    {
        PropertyType = propertyType;
        
        if (typeSymbol != null)
        {
            Namespace = typeSymbol.ContainingNamespace.ToDisplayString();
            IsNullable = typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;
        }
    }
    
    public UnrealProperty(PropertyType propertyType, UnrealType outer) : base(null, null!, outer)
    {
        PropertyType = propertyType;
    }
    
    public UnrealProperty(PropertyType type, string sourceName, Accessibility accessibility, UnrealType outer) 
        : base(sourceName, string.Empty, accessibility, string.Empty, outer)
    {
        PropertyType = type;
        IsPartial = false;
    }
    
    [Inspect(FullyQualifiedAttributeName = "UnrealSharp.Attributes.UPropertyAttribute", Name = "UPropertyAttribute")]
    public static UnrealType? UPropertyAttribute(UnrealType? outer, GeneratorAttributeSyntaxContext ctx, MemberDeclarationSyntax declaration, IReadOnlyList<AttributeData> attributes)
    {
        UnrealStruct owningStruct = (UnrealStruct) outer!;
        UnrealProperty property = PropertyFactory.CreateProperty(ctx.SemanticModel, declaration, outer!);
        owningStruct.Properties.List.Add(property);
        return property;
    }
    
    [InspectArgument(["PropertyFlags", "flags"], UPropertyAttributeName)]
    public static void PropertyFlagsSpecifier(UnrealType topType, TypedConstant flags)
    {
        UnrealProperty property = (UnrealProperty)topType;
        property.PropertyFlags |= (EPropertyFlags) flags.Value!;

        if (!property.PropertyFlags.HasFlag(EPropertyFlags.PersistentInstance | EPropertyFlags.InstancedReference))
        {
            return;
        }

        property.PropertyFlags |= InstancedFlags;
            
        if (topType.Outer is UnrealClass outerClass)
        {
            outerClass.ClassFlags |= EClassFlags.HasInstancedReference;
        }
        
        property.AddEditInlineMeta();
    }
    
    [InspectArgument("DefaultComponent", UPropertyAttributeName)]
    public static void DefaultComponentSpecifier(UnrealType topType, TypedConstant defaultComponent)
    {
        UnrealProperty property = (UnrealProperty)topType;
        property.DefaultComponent = (bool)defaultComponent.Value!;
        property.PropertyType = PropertyType.DefaultComponent;
        property.PropertyFlags |= EPropertyFlags.BlueprintVisible | EPropertyFlags.NonTransactional | EPropertyFlags.InstancedReference;
        property.AddEditInlineMeta();
    }
    
    [InspectArgument("RootComponent", UPropertyAttributeName)]
    public static void RootComponentSpecifier(UnrealType topType, TypedConstant rootComponent)
    {
        UnrealProperty property = (UnrealProperty)topType;
        property.RootComponent = (bool)rootComponent.Value!;
    }
    
    [InspectArgument("AttachmentComponent", UPropertyAttributeName)]
    public static void AttachmentComponentSpecifier(UnrealType topType, TypedConstant attachmentComponent)
    {
        UnrealProperty property = (UnrealProperty)topType;
        property.AttachmentComponent = (string)attachmentComponent.Value!;
    }
    
    [InspectArgument("AttachmentSocket", UPropertyAttributeName)]
    public static void AttachmentSocketSpecifier(UnrealType topType, TypedConstant attachmentSocket)
    {
        UnrealProperty property = (UnrealProperty)topType;
        property.AttachmentSocket = (string)attachmentSocket.Value!;
    }
    
    [InspectArgument("ReplicatedUsing", UPropertyAttributeName)]
    public static void ReplicatedUsingSpecifier(UnrealType topType, TypedConstant replicatedUsing)
    {
        UnrealProperty property = (UnrealProperty)topType;
        property.ReplicatedUsing = (string)replicatedUsing.Value!;
    }
    
    [InspectArgument("LifetimeCondition", UPropertyAttributeName)]
    public static void LifetimeConditionSpecifier(UnrealType topType, TypedConstant lifetimeCondition)
    {
        UnrealProperty property = (UnrealProperty)topType;
        property.LifetimeCondition = (ELifetimeCondition)lifetimeCondition.Value!;
    }
    
    [InspectArgument("BlueprintSetter", UPropertyAttributeName)]
    public static void BlueprintSetterSpecifier(UnrealType topType, TypedConstant blueprintSetter)
    {
        UnrealProperty property = (UnrealProperty)topType;
        property.BlueprintSetter = (string)blueprintSetter.Value!;
    }
    
    [InspectArgument("BlueprintGetter", UPropertyAttributeName)]
    public static void BlueprintGetterSpecifier(UnrealType topType, TypedConstant blueprintGetter)
    {
        UnrealProperty property = (UnrealProperty)topType;
        property.BlueprintGetter = (string)blueprintGetter.Value!;
    }
    
    [InspectArgument("Category", UPropertyAttributeName)]
    public static void CategorySpecifier(UnrealType topType, TypedConstant category)
    {
        UnrealProperty property = (UnrealProperty)topType;
        property.AddMetaData("Category", (string)category.Value!);
    }
    
    [InspectArgument("ArrayDim", UPropertyAttributeName)]
    public static void ArrayDimSpecifier(UnrealType topType, TypedConstant arrayDim)
    {
        UnrealProperty property = (UnrealProperty)topType;
        property.ArrayDim = (int)arrayDim.Value!;
    }

    public override void ExportType(GeneratorStringBuilder builder, SourceProductionContext spc)
    {
        string nullableSign = IsNullable ? "?" : string.Empty;
        string partialDeclaration = IsPartial ? "partial " : string.Empty;
        builder.AppendLine($"{Protection.AccessibilityToString()}{partialDeclaration}{ManagedType}{nullableSign} {SourceName}");
        builder.OpenBrace();
        ExportGetter(builder);
        ExportSetter(builder);
        builder.CloseBrace();
    }

    protected virtual void ExportGetter(GeneratorStringBuilder builder)
    {
        builder.AppendLine("get => ");
        ExportFromNative(builder, SourceGenUtilities.NativeObject);
    }
    
    protected virtual void ExportSetter(GeneratorStringBuilder builder)
    {
        builder.AppendLine("set => ");
        ExportToNative(builder, SourceGenUtilities.NativeObject, SourceGenUtilities.ValueParam);
    }

    public virtual void ExportBackingVariables(GeneratorStringBuilder builder, string nativeTypePtr)
    {
        string offsetCode = $"static int {OffsetVariable}";
        
        if (NeedsBackingFields)
        {
            ExportNativeProperty(builder, nativeTypePtr);
            
            string staticModifier = CanInstanceMarshallerBeStatic ? "static " : string.Empty;
            builder.AppendNewBackingField($"{staticModifier}{MarshallerType}? {InstancedMarshallerVariable};");
            builder.AppendNewBackingField($"{offsetCode} = CallGetPropertyOffset({NativePropertyVariable});");
        }
        else
        {
            builder.AppendNewBackingField($"{offsetCode} = CallGetPropertyOffsetFromName({nativeTypePtr}, \"{SourceName}\");");
        }
    }

    public void ExportNativeProperty(GeneratorStringBuilder builder, string nativeTypePtr)
    {
        builder.AppendNewBackingField($"static IntPtr {NativePropertyVariable} = CallGetNativePropertyFromName({nativeTypePtr}, \"{SourceName}\");");
    }
    
    protected string AppendOffsetMath(string basePtr)
    {
        return $"IntPtr.Add({basePtr}, {OffsetVariable})";
    }

    public virtual void ExportToNative(GeneratorStringBuilder builder, string buffer, string value)
    {
        AppendCallToNative(builder, MarshallerType, buffer, value);
    }
    
    public virtual void ExportFromNative(GeneratorStringBuilder builder, string buffer, string? assignmentOperator = null)
    {
        AppendCallFromNative(builder, MarshallerType, buffer, assignmentOperator);
    }
    
    protected void AppendCallToNative(GeneratorStringBuilder builder, string marshaller, string buffer, string value)
    {
        string offsetMathOperation = PropertyFlags.HasFlag(EPropertyFlags.ReturnParm) ? buffer : AppendOffsetMath(buffer);
        builder.Append($"{marshaller}.ToNative({offsetMathOperation}, 0, {value});");
    }
    
    protected void AppendCallFromNative(GeneratorStringBuilder builder, string marshaller, string buffer, string? assignmentOperator = null)
    {
        builder.Append($"{assignmentOperator}{marshaller}.FromNative({AppendOffsetMath(buffer)}, 0);");
    }
    
    public override void CreateTypeBuilder(GeneratorStringBuilder builder)
    {
        MakeProperty(builder, Outer!.BuilderNativePtr);
    }

    public virtual void MakeProperty(GeneratorStringBuilder builder, string ownerPtr)
    {
        byte propertyType = (byte) PropertyType;
        ulong flags = (ulong) PropertyFlags;
        int lifetimeCondition = (int) LifetimeCondition;

        string cacheOperation = CacheNativeTypePtr || HasAnyMetaData ? $"IntPtr {BuilderNativePtr} = " : string.Empty;
        builder.AppendLine($"{cacheOperation}NewProperty({ownerPtr}, {propertyType}, \"{SourceName}\", {flags}, \"{ReplicatedUsing}\", {ArrayDim}, {lifetimeCondition}, \"{BlueprintSetter}\", \"{BlueprintGetter}\");");
        
        AppendMetaData(builder, BuilderNativePtr);
    }
    
    private void AddEditInlineMeta() => AddMetaData("EditInline", "true");

    private string RefKindToString()
    {
        return RefKind switch
        {
            RefKind.None => string.Empty,
            RefKind.Ref => "ref ",
            RefKind.Out => "out ",
            RefKind.In => "in ",
            _ => string.Empty
        };
    }
}
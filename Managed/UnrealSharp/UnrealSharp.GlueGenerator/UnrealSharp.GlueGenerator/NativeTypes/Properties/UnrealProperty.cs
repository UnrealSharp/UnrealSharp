using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

public record struct PropertyMethod
{
    public PropertyMethod(Accessibility accessibility, UnrealFunction? customPropertyMethod = null)
    {
        Accessibility = accessibility;
        CustomPropertyMethod = customPropertyMethod;
    }
    
    public bool HasCustomMethod => CustomPropertyMethod != null;
    
    public readonly Accessibility Accessibility;
    public readonly UnrealFunction? CustomPropertyMethod;
}

[Inspector]
public record UnrealProperty : UnrealType
{
    private const string UPropertyAttributeName = "UPropertyAttribute";
    private const EPropertyFlags InstancedFlags = EPropertyFlags.InstancedReference | EPropertyFlags.ExportObject;

    public EPropertyFlags PropertyFlags = EPropertyFlags.None;
    public bool DefaultComponent;
    public bool IsRootComponent;
    public string AttachmentComponent = string.Empty;
    public string AttachmentSocket = string.Empty;
    public string ReplicatedUsing = string.Empty;
    public ELifetimeCondition LifetimeCondition = ELifetimeCondition.None;

    public readonly bool IsPartial = true;
    
    public bool IsBlittable = false;
    public RefKind ReferenceKind;

    public readonly bool IsNullable;
    
    public PropertyType PropertyType = PropertyType.Unknown;

    public FieldName ManagedType;
    
    public PropertyMethod? GetterMethod;
    public PropertyMethod? SetterMethod;

    public readonly bool IsRequired;
    
    public virtual string MarshallerType => throw new NotImplementedException();

    public bool NeedsBackingFields = false;
    public bool CanInstanceMarshallerBeStatic = false;
    
    public string OffsetVariable => $"{Outer!.SourceName}_{SourceName}_Offset";
    public string NativePropertyVariable => $"{Outer!.SourceName}_{SourceName}_Property";
    public string InstancedMarshallerVariable => $"{Outer!.SourceName}_{SourceName}_Marshaller";
    
    public string CallToNative => MarshallerType + ToNative;
    public string CallFromNative => MarshallerType + FromNative;
    
    protected string ToNative => ".ToNative";
    protected string FromNative => ".FromNative";
    
    public string GetParameterDeclaration() => $"{ReferenceKind.RefKindToString()}{ManagedType}{(IsNullable ? "?" : string.Empty)} {SourceName}";
    public string GetParameterCall() => $"{ReferenceKind.RefKindToString()}{SourceName}";

    public UnrealProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, PropertyType propertyType, UnrealType? outer = null, SyntaxNode? syntaxNode = null) : base(memberSymbol, outer)
    {
        PropertyType = propertyType;
        Namespace = typeSymbol.ContainingNamespace.ToDisplayString();
        IsNullable = typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;
        
        if (syntaxNode is PropertyDeclarationSyntax propertyDeclarationSyntax)
        {
            IPropertySymbol propertySymbol = (IPropertySymbol) memberSymbol;
            GetterMethod = propertySymbol.GetPropertyMethodInfo(this, propertyDeclarationSyntax, propertySymbol.GetMethod);
            SetterMethod = propertySymbol.GetPropertyMethodInfo(this, propertyDeclarationSyntax, propertySymbol.SetMethod);
            IsRequired = propertySymbol.IsRequired;
        }
    }
    
    public UnrealProperty(PropertyType propertyType, UnrealType outer) : base(outer)
    {
        PropertyType = propertyType;
    }
    
    public UnrealProperty(PropertyType type, string sourceName, Accessibility accessibility, UnrealType outer) 
        : base(sourceName, string.Empty, accessibility, string.Empty, outer)
    {
        PropertyType = type;
        IsPartial = false;
        GetterMethod = new PropertyMethod(Accessibility.NotApplicable);
        SetterMethod = new PropertyMethod(Accessibility.NotApplicable);
    }
    
    [Inspect(FullyQualifiedAttributeName = "UnrealSharp.Attributes.UPropertyAttribute", Name = "UPropertyAttribute")]
    public static UnrealType? UPropertyAttribute(UnrealType? outer, SyntaxNode? syntaxNode, GeneratorAttributeSyntaxContext ctx, ISymbol symbol, IReadOnlyList<AttributeData> attributes)
    {
        UnrealStruct owningStruct = (UnrealStruct) outer!;
        UnrealProperty property = PropertyFactory.CreateProperty(symbol, outer!, syntaxNode);
        owningStruct.Properties.List.Add(property);
        return property;
    }
    
    [InspectArgument(["PropertyFlags", "flags"], UPropertyAttributeName)]
    public static void PropertyFlagsSpecifier(UnrealType topType, TypedConstant flags)
    {
        UnrealProperty property = (UnrealProperty)topType;
        property.PropertyFlags |= (EPropertyFlags) flags.Value!;

        if (!property.PropertyFlags.HasFlag(EPropertyFlags.PersistentInstance))
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
        property.IsRootComponent = (bool)rootComponent.Value!;
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
    
    [InspectArgument("Category", UPropertyAttributeName)]
    public static void CategorySpecifier(UnrealType topType, TypedConstant category)
    {
        UnrealProperty property = (UnrealProperty)topType;
        property.AddMetaData("Category", (string)category.Value!);
    }

    public override void ExportType(GeneratorStringBuilder builder, SourceProductionContext spc)
    {
        if (this.HasCustomGetterOrSetter())
        {
            if (SetterMethod.HasCustomPropertyMethod())
            {
                SetterMethod!.Value.CustomPropertyMethod!.ExportType(builder, spc);
            }

            if (GetterMethod.HasCustomPropertyMethod())
            {
                GetterMethod!.Value.CustomPropertyMethod!.ExportType(builder, spc);
            }
            
            return;
        }
        
        string nullableSign = IsNullable ? "?" : string.Empty;
        string partialDeclaration = IsPartial ? "partial " : string.Empty;
        string isRequiredSign = IsRequired ? "required " : string.Empty;
        
        builder.AppendLine($"{TypeAccessibility.AccessibilityToString()}{isRequiredSign}{partialDeclaration}{ManagedType}{nullableSign} {SourceName}");
        builder.OpenBrace();
        
        if (GetterMethod != null)
        {
            builder.AppendGet(GetterMethod.Value.Accessibility);
            ExportGetter(builder);
        }
        
        if (SetterMethod != null)
        {
            builder.AppendSet(SetterMethod.Value.Accessibility);
            ExportSetter(builder);
        }
        
        builder.CloseBrace();
    }

    protected virtual void ExportGetter(GeneratorStringBuilder builder)
    {
        builder.Append(" => ");
        ExportFromNative(builder, SourceGenUtilities.NativeObject);
    }
    
    protected virtual void ExportSetter(GeneratorStringBuilder builder)
    {
        builder.Append(" => ");
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
        return $"{basePtr} + {OffsetVariable}";
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
        string offsetMathOperation = PropertyFlags.IsReturnValue() ? buffer : AppendOffsetMath(buffer);
        builder.Append($"{marshaller}.ToNative({offsetMathOperation}, 0, {value});");
    }
    
    protected void AppendCallFromNative(GeneratorStringBuilder builder, string marshaller, string buffer, string? assignmentOperator = null)
    {
        builder.Append($"{assignmentOperator}{marshaller}.FromNative({AppendOffsetMath(buffer)}, 0);");
    }

    public override void PopulateJsonObject(JsonObject jsonObject)
    {
        base.PopulateJsonObject(jsonObject);
        jsonObject.TrySetJsonEnum("PropertyFlags", PropertyFlags);
        jsonObject.TrySetJsonEnum("PropertyType", PropertyType);
        jsonObject.TrySetJsonBoolean("DefaultComponent", DefaultComponent);
        jsonObject.TrySetJsonBoolean("IsRootComponent", IsRootComponent);
        jsonObject.TrySetJsonString("AttachmentComponent", AttachmentComponent);
        jsonObject.TrySetJsonString("AttachmentSocket", AttachmentSocket);
        jsonObject.TrySetJsonString("ReplicatedUsing", ReplicatedUsing);
        jsonObject.TrySetJsonEnum("LifetimeCondition", LifetimeCondition);
        
        SetGetterSetterToJson(jsonObject, "GetterMethod", GetterMethod);
        SetGetterSetterToJson(jsonObject, "SetterMethod", SetterMethod);
    }
    
    private void SetGetterSetterToJson(JsonObject jsonObject, string key, PropertyMethod? method)
    {
        if (method == null || !method.Value.HasCustomMethod)
        {
            return;
        }
        
        JsonObject methodObject = new JsonObject();
        method.Value.CustomPropertyMethod!.PopulateJsonObject(methodObject);
        jsonObject[key] = methodObject;
    }
}
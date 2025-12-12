using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public readonly record struct PropertyMethod
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
    // Constants
    private const string UPropertyAttributeName = "UPropertyAttribute";
    private const EPropertyFlags InstancedFlags = EPropertyFlags.InstancedReference | EPropertyFlags.ExportObject;

    // General property configuration
    public EPropertyFlags PropertyFlags = EPropertyFlags.None;
    public bool DefaultComponent;
    public bool IsRootComponent;
    public string AttachmentComponent = string.Empty;
    public string AttachmentSocket = string.Empty;
    public string ReplicatedUsing = string.Empty;
    public ELifetimeCondition LifetimeCondition = ELifetimeCondition.None;

    // Immutable metadata
    public readonly bool IsPartial = true;
    public readonly bool IsNullable;
    public readonly bool IsRequired;

    // Type and marshaling information
    public PropertyType PropertyType = PropertyType.Unknown;
    public FieldName ManagedType;
    public RefKind ReferenceKind;

    public bool CanInstanceMarshallerBeStatic = false;
    
    public virtual string MarshallerType => throw new NotImplementedException();
    public virtual bool NeedsCachedMarshaller => false;
    public virtual bool NeedsBackingNativeProperty => false;
    public virtual bool IsBlittable => false;

    // Getter / Setter info
    public PropertyMethod? GetterMethod;
    public PropertyMethod? SetterMethod;

    // Codegen variables
    public string OffsetVariable => $"{Outer!.SourceName}_{SourceName}_Offset";
    public string NativePropertyVariable => $"{Outer!.SourceName}_{SourceName}_Property";
    public string InstancedMarshallerVariable => $"{Outer!.SourceName}_{SourceName}_Marshaller";

    protected string ToNative => ".ToNative";
    protected string FromNative => ".FromNative";

    public string CallToNative => MarshallerType + ToNative;
    public string CallFromNative => MarshallerType + FromNative;
    
    public virtual string NullValue => $"default({ManagedType})";

    // Parameter helpers
    public string GetParameterDeclaration() => $"{ReferenceKind.RefKindToString()}{ManagedType}{(IsNullable ? "?" : string.Empty)} {SourceName}";
    public string GetParameterCall() => $"{ReferenceKind.RefKindToString()}{SourceName}";
    
    public UnrealProperty(ISymbol memberSymbol, ITypeSymbol typeSymbol, PropertyType propertyType, UnrealType? outer = null, SyntaxNode? syntaxNode = null) : base(memberSymbol, outer, syntaxNode)
    {
        PropertyType = propertyType;
        Namespace = typeSymbol.GetNamespace();
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
        : base(sourceName, outer.Namespace, accessibility, outer.AssemblyName, outer)
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
        ExportBackingVariables(builder);
        builder.AppendLine();
        
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

    public override void ExportBackingVariables(GeneratorStringBuilder builder)
    {
        string offsetCode = $"static int {OffsetVariable}";
        
        if (NeedsBackingNativeProperty || NeedsCachedMarshaller)
        {
            ExportNativeProperty(builder);
        }
        
        if (NeedsCachedMarshaller)
        {
            string staticModifier = CanInstanceMarshallerBeStatic ? "static " : string.Empty;
            builder.AppendNewBackingField($"{staticModifier}{MarshallerType}? {InstancedMarshallerVariable};");
            builder.AppendNewBackingField($"{offsetCode};");
        }
        else
        {
            builder.AppendNewBackingField($"{offsetCode};");
        }
    }

    public override void ExportBackingVariablesToStaticConstructor(GeneratorStringBuilder builder, string nativeType)
    {
        if (NeedsBackingNativeProperty || NeedsCachedMarshaller)
        {
            builder.AppendLine($"{NativePropertyVariable} = CallGetNativePropertyFromName({nativeType}, \"{SourceName}\");"); 
        }
        
        if (NeedsCachedMarshaller)
        {
            builder.AppendLine($"{OffsetVariable} = CallGetPropertyOffset({NativePropertyVariable});");
        }
        else
        {
            builder.AppendLine($"{OffsetVariable} = CallGetPropertyOffsetFromName({nativeType}, \"{SourceName}\");");
        }

        if (SetterMethod.HasCustomPropertyMethod())
        {
            SetterMethod!.Value.CustomPropertyMethod!.ExportBackingVariablesToStaticConstructor(builder, nativeType);
        }

        if (GetterMethod.HasCustomPropertyMethod())
        {
            GetterMethod!.Value.CustomPropertyMethod!.ExportBackingVariablesToStaticConstructor(builder, nativeType);
        }
    }

    public void ExportNativeProperty(GeneratorStringBuilder builder)
    {
        builder.AppendNewBackingField($"static IntPtr {NativePropertyVariable};");
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
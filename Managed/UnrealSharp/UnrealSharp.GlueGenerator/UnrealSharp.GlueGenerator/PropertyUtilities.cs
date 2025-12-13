using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnrealSharp.GlueGenerator.NativeTypes;
using UnrealSharp.GlueGenerator.NativeTypes.Properties;

namespace UnrealSharp.GlueGenerator;

public static class PropertyUtilities
{
    public const EPropertyFlags BaseParametersFlags = EPropertyFlags.Parm | EPropertyFlags.BlueprintVisible | EPropertyFlags.BlueprintReadOnly;

    public const EPropertyFlags OutParameterFlags = BaseParametersFlags | EPropertyFlags.OutParm;
    public const EPropertyFlags ReturnParameterFlags = OutParameterFlags | EPropertyFlags.ReturnParm;

    public const EPropertyFlags BaseBlueprintVisiblePropertyFlags = EPropertyFlags.BlueprintVisible;

    public const EPropertyFlags BaseBlueprintReadOnlyPropertyFlags = EPropertyFlags.BlueprintVisible | EPropertyFlags.BlueprintReadOnly;

    public const EPropertyFlags BaseBlueprintReadWritePropertyFlags = EPropertyFlags.BlueprintVisible | EPropertyFlags.BlueprintReadWrite;

    public static void MakeParameter(this UnrealProperty property)
    {
        property.PropertyFlags |= BaseParametersFlags;
    }

    public static void MakeReturnParameter(this UnrealProperty property)
    {
        property.PropertyFlags |= ReturnParameterFlags;
    }

    public static void MakeOutParameter(this UnrealProperty property)
    {
        property.PropertyFlags |= OutParameterFlags;
    }

    public static void MakeBlueprintVisibleProperty(this UnrealProperty property)
    {
        property.PropertyFlags |= BaseBlueprintVisiblePropertyFlags;
    }

    public static void MakeBlueprintReadOnlyProperty(this UnrealProperty property)
    {
        property.PropertyFlags |= BaseBlueprintReadOnlyPropertyFlags;
    }

    public static void MakeBlueprintReadWriteProperty(this UnrealProperty property)
    {
        property.PropertyFlags |= BaseBlueprintReadWritePropertyFlags;
    }

    public static void MakeBlueprintAssignable(this UnrealProperty property)
    {
        property.PropertyFlags |= EPropertyFlags.BlueprintAssignable;
    }

    public static PropertyMethod? GetPropertyMethodInfo(this IPropertySymbol property, UnrealProperty unrealProperty, PropertyDeclarationSyntax declarationSyntax, IMethodSymbol? getterOrSetter)
    {
        if (getterOrSetter == null)
        {
            return null;
        }
        
        UnrealFunction? customGetterSetter = null;
        if (declarationSyntax.HasCustomGetterOrSetter())
        { 
            customGetterSetter = new UnrealGetterSetterFunction(unrealProperty, getterOrSetter, unrealProperty.Outer!);
        }
        
        bool isDifferentAccessibility = getterOrSetter.DeclaredAccessibility != property.DeclaredAccessibility;
        Accessibility accessibility = isDifferentAccessibility ? getterOrSetter.DeclaredAccessibility : Accessibility.NotApplicable;
        
        return new PropertyMethod(accessibility, customGetterSetter);
    }

    public static void AddEditInlineMeta(this UnrealProperty property) => property.AddMetaData("EditInline", "true");

    public static bool IsReturnValue(this EPropertyFlags propertyFlags) => propertyFlags.HasFlag(EPropertyFlags.ReturnParm);

    public static bool HasCustomGetterOrSetter(this PropertyDeclarationSyntax propertyDeclaration)
    {
        AccessorListSyntax? accessorList = propertyDeclaration.AccessorList;

        if (accessorList == null)
        {
            return false;
        }

        foreach (var accessor in accessorList.Accessors)
        {
            if (accessor.Body != null || accessor.ExpressionBody != null)
            {
                return true;
            }
        }

        return false;
    }
    
    public static bool HasCustomGetterOrSetter(this UnrealProperty property)
    {
        return property.GetterMethod.HasCustomPropertyMethod() || property.SetterMethod.HasCustomPropertyMethod();
    }
    
    public static bool HasCustomPropertyMethod(this PropertyMethod? method)
    {
        return method != null && method.Value.CustomPropertyMethod != null;
    }
    
    public static string GetNullableAnnotation(this UnrealProperty property)
    {
        return property.IsNullable ? "?" : string.Empty;
    }
}
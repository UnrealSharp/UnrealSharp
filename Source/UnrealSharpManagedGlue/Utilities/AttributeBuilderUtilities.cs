using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.Attributes;

namespace UnrealSharpManagedGlue.Utilities;

public static class AttributeBuilderUtilities
{ 
    public static void AddIsBlittableAttribute(this AttributeBuilder attributeBuilder)
    {
        attributeBuilder.AddAttribute("BlittableType");
    }

    public static void AddStructLayoutAttribute(this AttributeBuilder attributeBuilder, System.Runtime.InteropServices.LayoutKind layoutKind)
    {
        attributeBuilder.AddAttribute("StructLayout");
        attributeBuilder.AddArgument($"LayoutKind.{layoutKind}");
    }

    public static void AddGeneratedFunctionName(this AttributeBuilder attributeBuilder, UhtFunction function)
    {
        attributeBuilder.AddAttribute("GeneratedFunction");
        attributeBuilder.AddArgument($"\"{function.SourceName}\"");
    }
}
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes;

[Inspector]
public record UnrealInterface : UnrealClassBase
{
    public override FieldType FieldType => FieldType.Interface;
    
    private const string UInterfaceAttributeName = "UInterfaceAttribute";

    public UnrealInterface(ITypeSymbol typeSymbol, UnrealType? outer = null) : base(typeSymbol, outer)
    {
        ClassFlags |= EClassFlags.Interface;
        AddMetaData("BlueprintType", "true");
    }
    
    [Inspect("UnrealSharp.Attributes.UInterfaceAttribute", "UInterfaceAttribute", "Global")]
    public static UnrealType UInterfaceAttribute(UnrealType? outer, SyntaxNode? syntaxNode, GeneratorAttributeSyntaxContext ctx, ISymbol symbol, IReadOnlyList<AttributeData> attributes)
    {
        ITypeSymbol typeSymbol = (ITypeSymbol) symbol;
        UnrealInterface unrealClass = new UnrealInterface(typeSymbol);
        return unrealClass;
    }

    [InspectArgument("CannotImplementInterfaceInBlueprint", UInterfaceAttributeName)]
    public static void CannotImplementInterfaceInBlueprintSpecifier(UnrealType interfaceType, TypedConstant value)
    {
        UnrealInterface unrealInterface = (UnrealInterface) interfaceType;
        
        bool boolValue = (bool) value.Value!;
        unrealInterface.AddMetaData("CannotImplementInterfaceInBlueprint", boolValue ? "true" : "false");
    }

    public override void ExportType(GeneratorStringBuilder builder, SourceProductionContext spc)
    {
        ExportMarshaller(builder);
    }
    
    private void ExportMarshaller(GeneratorStringBuilder builder)
    {
        builder.AppendLine($"public static class {SourceName}Marshaller");
        builder.OpenBrace();
        
        string marshallerDeclaration = $"UnrealSharp.CoreUObject.ScriptInterfaceMarshaller<{SourceName}>";
        
        builder.AppendLine($"public static void ToNative(IntPtr nativeBuffer, int arrayIndex, {SourceName} obj) => {marshallerDeclaration}.ToNative(nativeBuffer, arrayIndex, obj);");
        builder.AppendLine($"public static {SourceName} FromNative(IntPtr nativeBuffer, int arrayIndex) => {marshallerDeclaration}.FromNative(nativeBuffer, arrayIndex);");
        
        builder.CloseBrace();
    }
}
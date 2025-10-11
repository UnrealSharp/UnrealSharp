using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnrealSharp.GlueGenerator.NativeTypes;

[Inspector]
public record UnrealInterface : UnrealClassBase
{
    private const string UInterfaceAttributeName = "UInterfaceAttribute";
    public override int FieldTypeValue => 3;

    public UnrealInterface(ITypeSymbol typeSymbol, SyntaxNode syntax, UnrealType? outer = null) : base(typeSymbol, syntax, outer)
    {
        ClassFlags |= EClassFlags.Interface;
    }
    
    [Inspect("UnrealSharp.Attributes.UInterfaceAttribute", "UInterfaceAttribute", "Global")]
    public static UnrealType? UInterfaceAttribute(UnrealType? outer, GeneratorAttributeSyntaxContext ctx, MemberDeclarationSyntax declarationSyntax, IReadOnlyList<AttributeData> attributes)
    {
        UnrealInterface unrealClass = new UnrealInterface((ITypeSymbol) ctx.TargetSymbol, declarationSyntax, outer);
        InspectorManager.InspectSpecifiers(UInterfaceAttributeName, unrealClass, attributes);
        InspectorManager.InspectTypeMembers(unrealClass, declarationSyntax, ctx);
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
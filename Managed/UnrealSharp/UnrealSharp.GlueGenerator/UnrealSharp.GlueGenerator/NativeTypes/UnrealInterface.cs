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
        UnrealInterface unrealInterface = new UnrealInterface(typeSymbol);
        return unrealInterface;
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
        TypeDeclarationBuilder typeBuilder = TypeDeclarationBuilder.FromUnrealType(this, SourceGenUtilities.InterfaceKeyword);
        
        typeBuilder.Build(builder);
        
        builder.AppendLine($"static {SourceName} Wrap(UnrealSharp.CoreUObject.UObject obj)");
        builder.OpenBrace();
        builder.AppendLine($"return new {SourceName}Wrapper(obj);");
        builder.CloseBrace();

        ExportImplementsMethod(builder);

        builder.CloseBrace();

        ExportWrapperClass(builder);
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

    private void ExportImplementsMethod(GeneratorStringBuilder builder)
    {
        builder.AppendLine("public static bool Implements(UnrealSharp.Core.UnrealSharpObject? obj) => obj != null && (UObjectExporter.CallImplementsInterface(obj.NativeObject, NativeTypePtr).ToManagedBool());");
    }

    private void ExportWrapperClass(GeneratorStringBuilder builder)
    {
        string wrapperName = $"{SourceName}Wrapper";
        
        TypeDeclarationBuilder typeDeclarationBuilder = TypeDeclarationBuilder
            .FromUnrealType(this, SourceGenUtilities.ClassKeyword)
            .WithDeclarationName(wrapperName)
            .Accessibility("file ")
            .Implements(SourceName)
            .Implements("UnrealSharp.CoreUObject.IScriptInterface");
        
        typeDeclarationBuilder.Build(builder);
        
        builder.AppendLine("public UnrealSharp.CoreUObject.UObject Object { get; }");
        builder.AppendLine("private IntPtr NativeObject => Object.NativeObject;");
        builder.AppendLine($"public {wrapperName}(UnrealSharp.CoreUObject.UObject obj) => Object = obj;");
        
        builder.BeginTypeStaticConstructor(wrapperName);
        foreach (UnrealFunctionBase function in Functions.List)
        {
            UnrealFunction unrealFunction = (UnrealFunction) function;
            unrealFunction.ExportBackingVariablesToStaticConstructor(builder, SourceGenUtilities.NativeTypePtr);
        }
        builder.EndTypeStaticConstructor();
        
        foreach (UnrealFunctionBase function in Functions.List)
        {
            UnrealFunction unrealFunction = (UnrealFunction) function;
            unrealFunction.ExportBackingVariables(builder);
            unrealFunction.ExportWrapperMethod(builder, string.Empty);
        }
        
        builder.CloseBrace();
    }
}
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnrealSharp.GlueGenerator.NativeTypes.Properties;

namespace UnrealSharp.GlueGenerator.NativeTypes;

[Inspector]
public record UnrealScriptStruct : UnrealStruct
{
    public override int FieldTypeValue => 1;
    
    public UnrealScriptStruct(ISymbol typeSymbol, SyntaxNode syntax, UnrealType? outer = null) : base(typeSymbol, syntax, outer)
    {
    }
    
    [Inspect("UnrealSharp.Attributes.UStructAttribute", "UStructAttribute", "Global")]
    public static UnrealType? UStructAttribute(UnrealType? outer, GeneratorAttributeSyntaxContext ctx, MemberDeclarationSyntax declarationSyntax, IReadOnlyList<AttributeData> attributes)
    {
        UnrealScriptStruct unrealStruct = new UnrealScriptStruct(ctx.TargetSymbol, declarationSyntax, outer);
        InspectorManager.InspectSpecifiers("UStructAttribute", unrealStruct, attributes);
        InspectorManager.InspectTypeMembers(unrealStruct, declarationSyntax, ctx);
        return unrealStruct;
    }

    public override void ExportType(GeneratorStringBuilder builder, SourceProductionContext spc)
    {
        builder.BeginType(this, TypeKind.Struct, interfaceDeclarations: [$"MarshalledStruct<{SourceName}>"]);
        
        bool isStructBlittable = true;
        int propertyCount = Properties.Count;
        for (int i = 0; i < propertyCount; i++)
        {
            UnrealProperty property = Properties.List[i];
            
            property.ExportBackingVariables(builder, SourceGenUtilities.NativeTypePtr);
            isStructBlittable &= property.IsBlittable;
        }
        
        builder.AppendLine();
        
        builder.AppendLine($"private static int NativeDataSize = UScriptStructExporter.CallGetNativeStructSize({SourceGenUtilities.NativeTypePtr});");
        builder.AppendLine("public static int GetNativeDataSize() => NativeDataSize;");
        builder.AppendLine($"public static IntPtr GetNativeClassPtr() => {SourceGenUtilities.NativeTypePtr};");
        
        builder.AppendLine($"public static {SourceName} FromNative(IntPtr buffer) => new {SourceName}(buffer);");
        
        builder.AppendLine();
        builder.AppendLine("public void ToNative(IntPtr buffer)");
        builder.OpenBrace();
        builder.BeginUnsafeBlock();
        builder.AppendLine();

        if (isStructBlittable)
        {
            builder.AppendLine("BlittableMarshaller<" + SourceName + ">.ToNative(buffer, 0, this);");
        }
        else
        {
            for (int i = 0; i < propertyCount; i++)
            {
                UnrealProperty property = Properties.List[i];
                property.ExportToNative(builder, SourceGenUtilities.Buffer, property.SourceName);
                builder.AppendLine();
            }
        }
        
        builder.EndUnsafeBlock();
        builder.CloseBrace();
        builder.AppendLine();
        
        builder.AppendLine($"public {SourceName}(IntPtr buffer)");
        builder.OpenBrace();
        builder.BeginUnsafeBlock();
        builder.AppendLine();
        
        if (isStructBlittable)
        {
            builder.AppendLine($"this = BlittableMarshaller<{SourceName}>.FromNative(buffer, 0);");
        }
        else
        {
            for (int i = 0; i < propertyCount; i++)
            {
                UnrealProperty property = Properties.List[i];
                property.ExportFromNative(builder, SourceGenUtilities.Buffer, $"{property.SourceName} = ");
                builder.AppendLine();
            }
        }
        
        builder.EndUnsafeBlock();
        builder.CloseBrace();
        builder.CloseBrace();
        
        MakeMarshaller(builder);
    }

    public override void CreateTypeBuilder(GeneratorStringBuilder builder)
    {
        base.CreateTypeBuilder(builder);
        AppendProperties(builder, Properties.List);
    }

    void MakeMarshaller(GeneratorStringBuilder builder)
    {
        builder.AppendLine($"public static class {SourceName}Marshaller");
        builder.OpenBrace();
        builder.AppendLine($"public static void ToNative(IntPtr buffer, int index, {SourceName} obj) => obj.ToNative(buffer + (index * {SourceName}.GetNativeDataSize()));");
        builder.AppendLine($"public static {SourceName} FromNative(IntPtr buffer, int index) => new {SourceName}(buffer + (index * {SourceName}.GetNativeDataSize()));");
        builder.CloseBrace();
    }
}
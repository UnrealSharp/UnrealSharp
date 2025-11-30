using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using UnrealSharp.GlueGenerator.NativeTypes.Properties;

namespace UnrealSharp.GlueGenerator.NativeTypes;

[Inspector]
public record UnrealScriptStruct : UnrealStruct
{
    public override int FieldTypeValue => 1;
    public readonly bool IsRecord;
    
    public UnrealScriptStruct(ISymbol symbol, UnrealType? outer = null) : base(symbol, outer)
    {
        ITypeSymbol typeSymbol = (ITypeSymbol) symbol;
        IsRecord = typeSymbol.IsRecord;
    }
    
    [Inspect("UnrealSharp.Attributes.UStructAttribute", "UStructAttribute", "Global")]
    public static UnrealType? UStructAttribute(UnrealType? outer, SyntaxNode? syntaxNode, GeneratorAttributeSyntaxContext ctx, ISymbol symbol, IReadOnlyList<AttributeData> attributes)
    {
        UnrealScriptStruct unrealStruct = new UnrealScriptStruct(symbol, outer);
        InspectorManager.InspectSpecifiers("UStructAttribute", unrealStruct, attributes);

        ITypeSymbol typeSymbol = (ITypeSymbol) symbol;
        if (unrealStruct.IsRecord)
        {
            InspectorManager.InspectTypeMembers(unrealStruct, typeSymbol, ctx);
        }
        else
        {
            InspectorManager.InspectTypeMembers(unrealStruct, syntaxNode, typeSymbol, ctx);
        }
        
        return unrealStruct;
    }

    public override void ExportType(GeneratorStringBuilder builder, SourceProductionContext spc)
    {
        string recordModifier = IsRecord ? "record " : string.Empty;
        builder.BeginType(this, SourceGenUtilities.StructKeyword, modifiers: recordModifier, interfaceDeclarations: [$"MarshalledStruct<{SourceName}>"]);
        
        ExportBackingVariables(builder);
        
        builder.BeginTypeStaticConstructor(this);
        ExportBackingVariablesToStaticConstructor(builder, SourceGenUtilities.NativeTypePtr);
        builder.EndTypeStaticConstructor();
        
        bool isStructBlittable = true;
        foreach (UnrealProperty property in Properties.List)
        {
            isStructBlittable &= property.IsBlittable;
        }
        
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
            foreach (UnrealProperty property in Properties.List)
            {
                property.ExportToNative(builder, SourceGenUtilities.Buffer, property.SourceName);
                builder.AppendLine();
            }
        }
        
        builder.EndUnsafeBlock();
        builder.CloseBrace();
        builder.AppendLine();
        
        builder.AppendLine($"public {SourceName}(IntPtr buffer)");
        
        if (IsRecord)
        {
            string constructorArgs = string.Join(", ", Properties.List.Select(p => p.NullValue));
            builder.Append($" : this({constructorArgs})");
        }
        
        builder.OpenBrace();
        builder.BeginUnsafeBlock();
        builder.AppendLine();
        
        if (isStructBlittable)
        {
            builder.AppendLine($"this = BlittableMarshaller<{SourceName}>.FromNative(buffer, 0);");
        }
        else
        {
            foreach (UnrealProperty property in Properties.List)
            {
                property.ExportFromNative(builder, SourceGenUtilities.Buffer, $"{property.SourceName} = ");
                builder.AppendLine();
            }
        }
        
        builder.EndUnsafeBlock();
        builder.CloseBrace();
        builder.CloseBrace();
        
        MakeMarshaller(builder);
    }

    public override void ExportBackingVariables(GeneratorStringBuilder builder)
    {
        base.ExportBackingVariables(builder);
        builder.AppendNewBackingField("static int NativeDataSize;");
    }

    public override void ExportBackingVariablesToStaticConstructor(GeneratorStringBuilder builder, string nativeType)
    {
        base.ExportBackingVariablesToStaticConstructor(builder, nativeType);
        builder.AppendLine($"NativeDataSize = UScriptStructExporter.CallGetNativeStructSize({SourceGenUtilities.NativeTypePtr});");
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
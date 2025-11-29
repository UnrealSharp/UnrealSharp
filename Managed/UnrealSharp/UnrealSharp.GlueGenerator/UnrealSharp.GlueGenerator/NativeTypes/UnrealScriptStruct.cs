using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnrealSharp.GlueGenerator.NativeTypes.Properties;

namespace UnrealSharp.GlueGenerator.NativeTypes;

[Inspector]
public record UnrealScriptStruct : UnrealStruct
{
    public override int FieldTypeValue => 1;
    public bool IsRecord;
    
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
        builder.BeginType(this, TypeKind.Struct, modifiers: recordModifier, interfaceDeclarations: [$"MarshalledStruct<{SourceName}>"]);
        
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
        
        if (IsRecord)
        {
            string constructorArgs = string.Join(", ", Properties.List.Select(p => $"default({p.ManagedType})"));
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

    void MakeMarshaller(GeneratorStringBuilder builder)
    {
        builder.AppendLine($"public static class {SourceName}Marshaller");
        builder.OpenBrace();
        builder.AppendLine($"public static void ToNative(IntPtr buffer, int index, {SourceName} obj) => obj.ToNative(buffer + (index * {SourceName}.GetNativeDataSize()));");
        builder.AppendLine($"public static {SourceName} FromNative(IntPtr buffer, int index) => new {SourceName}(buffer + (index * {SourceName}.GetNativeDataSize()));");
        builder.CloseBrace();
    }
}
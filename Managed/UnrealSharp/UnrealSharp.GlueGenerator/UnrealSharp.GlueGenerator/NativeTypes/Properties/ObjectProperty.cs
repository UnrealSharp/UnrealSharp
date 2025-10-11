﻿using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

public record ObjectProperty : FieldProperty
{
    public override string MarshallerType => DefaultComponent
        ? $"DefaultComponentMarshaller<{ManagedType}>"
        : $"ObjectMarshaller<{ManagedType}>";

    public ObjectProperty(SyntaxNode syntaxNode, ISymbol memberSymbol, ITypeSymbol? typeSymbol, UnrealType outer) 
        : base(syntaxNode, memberSymbol, typeSymbol, PropertyType.Object, outer)
    {
        CacheNativeTypePtr = true;
    }
    
    public ObjectProperty(string managedType, string sourceName, Accessibility accessibility, UnrealType outer) 
        : base(PropertyType.Object, managedType, sourceName, accessibility, outer)
    {
        CacheNativeTypePtr = true;
    }

    public override void ExportFromNative(GeneratorStringBuilder builder, string buffer, string? assignmentOperator = null)
    {
        if (DefaultComponent)
        {
            builder.Append($"{MarshallerType}{FromNative}(this, \"{SourceName}\", {AppendOffsetMath(SourceGenUtilities.NativeObject)}, 0);");
        }
        else
        {
            base.ExportFromNative(builder, buffer, assignmentOperator);
        }
    }

    public override void MakeProperty(GeneratorStringBuilder builder, string ownerPtr)
    {
        base.MakeProperty(builder, ownerPtr);
        
        if (DefaultComponent)
        {
            string trueOrFalse = RootComponent ? "true" : "false";
            builder.AppendLine($"ModifyDefaultComponent({BuilderNativePtr}, {trueOrFalse}, \"{AttachmentComponent}\", \"{AttachmentSocket}\");");
        }
    }
}
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using UnrealSharp.GlueGenerator.NativeTypes;

namespace UnrealSharp.GlueGenerator;

public static class StringBuilderExtensions
{   
    public static void AppendNewBackingField(this GeneratorStringBuilder builder, string declaration)
    {
        builder.AppendEditorBrowsableAttribute();
        builder.Append(declaration);
    }
    
    public static void DeclareDirective(this GeneratorStringBuilder builder, string directive)
    {
        builder.AppendLine($"using {directive};");
    }
    
    public static void DeclareDirectives(this GeneratorStringBuilder builder, List<string> directives)
    {
        for (int i = 0; i < directives.Count; i++)
        {
            builder.DeclareDirective(directives[i]);
        }
    }
    
    public static void BeginGeneratedSourceFile(this GeneratorStringBuilder builder, UnrealType type)
    {
        builder.AppendLine("#nullable enable");
        builder.AppendLine("using UnrealSharp.Interop;");
        builder.AppendLine("using static UnrealSharp.Interop.FTypeBuilderExporter;");
        builder.AppendLine("using static UnrealSharp.Interop.FPropertyExporter;"); 
        builder.AppendLine("using static System.ComponentModel.EditorBrowsableState;");  
        builder.AppendLine("using static UnrealSharp.Interop.UClassExporter;");
        builder.AppendLine("using static UnrealSharp.Interop.UFunctionExporter;");
        
        builder.AppendLine("using System.ComponentModel;");
        builder.AppendLine("using UnrealSharp;");
        builder.AppendLine("using UnrealSharp.Core.Marshallers;");
        builder.AppendLine("using UnrealSharp.Core.Attributes;");
        builder.AppendLine();
        builder.AppendLine($"namespace {type.Namespace};");
        builder.AppendLine();
    }

    public static void BeginModuleInitializer(this GeneratorStringBuilder builder, UnrealType type)
    {
        builder.StartModuleInitializer(type.SourceName);
        builder.StartModuleInitializerMethod(type);
        
        builder.AppendLine($"IntPtr {type.BuilderNativePtr} = NewType(\"{type.EngineName}\", {type.FieldTypeValue}, typeof({type.FullName}), out var needsRebuild);");
        builder.AppendLine("if (!needsRebuild) return;");
        type.CreateTypeBuilder(builder);
        builder.EndModuleInitializer();
    }
    
    public static void BeginType(this GeneratorStringBuilder builder, UnrealType type, TypeKind typeKind, string nativeTypePtrName = SourceGenUtilities.NativeTypePtr, string overrideTypeName = "", string baseType = "", List<string>? interfaceDeclarations = null)
    {
        string typeKeyWord = typeKind switch
        {
            TypeKind.Class => "class",
            TypeKind.Struct => "struct",
            TypeKind.Enum => "enum",
            TypeKind.Interface => "interface",
            _ => throw new Exception("Unsupported type kind passed to BeginType" + typeKind)
        };
        
        string declarationName = string.IsNullOrEmpty(overrideTypeName) ? type.SourceName : overrideTypeName;
        builder.AppendLine($"[GeneratedType(\"{type.EngineName}\", \"{type.FullName}\")]");
        builder.AppendLine($"public partial {typeKeyWord} {declarationName}");
        
        if (!string.IsNullOrEmpty(baseType))
        {
            builder.Append($" : {baseType}");
        }
        
        if (interfaceDeclarations != null && interfaceDeclarations.Count > 0)
        {
            builder.Append(" : ");
            for (int i = 0; i < interfaceDeclarations.Count; i++)
            {
                builder.Append(interfaceDeclarations[i]);
                if (i < interfaceDeclarations.Count - 1)
                {
                    builder.Append(", ");
                }
            }
        }
        
        builder.OpenBrace();
        
        string engineName = string.IsNullOrEmpty(overrideTypeName) ? type.EngineName : overrideTypeName;
        builder.AppendNewBackingField($"static IntPtr {nativeTypePtrName} = UCoreUObjectExporter.CallGetType(\"{type.AssemblyName}\", \"{type.Namespace}\", \"{engineName}\");");
    }

    public static void StartModuleInitializer(this GeneratorStringBuilder builder, string outerName)
    {
        builder.AppendLine();
        builder.AppendLine($"static class {outerName}_Initializer");
        builder.OpenBrace();
    }
    
    public static void StartModuleInitializerMethod(this GeneratorStringBuilder builder, UnrealType type)
    {
        builder.AppendLine("#pragma warning disable CA2255");
        builder.AppendLine("[System.Runtime.CompilerServices.ModuleInitializer]");
        builder.AppendLine("#pragma warning restore CA2255");
        builder.AppendLine($"public static void Register() => UnrealSharp.Core.StartUpJobManager.RegisterStartUpJob(\"{type.AssemblyName}\", Initialize);");
        builder.AppendLine("public static void Initialize()");
        builder.OpenBrace();
    }
    
    public static void EndModuleInitializer(this GeneratorStringBuilder builder)
    {
        builder.CloseBrace();
        builder.CloseBrace();
    }
    
    public static void AllocateParameterBuffer(this GeneratorStringBuilder builder, string sizeName)
    {
        builder.AppendLine($"byte* {SourceGenUtilities.ParamsBufferAllocation} = stackalloc byte[{sizeName}];");
        builder.AppendLine($"nint {SourceGenUtilities.ParamsBuffer} = (nint) {SourceGenUtilities.ParamsBufferAllocation};");
    }
    
    public static void AppendEditorBrowsableAttribute(this GeneratorStringBuilder builder)
    {
        builder.AppendLine("[EditorBrowsable(Never)]");
    }
    
    public static void BeginPreproccesorBlock(this GeneratorStringBuilder builder, string condition)
    {
        builder.AppendLine($"#if {condition}");
    }
    
    public static void EndPreproccesorBlock(this GeneratorStringBuilder builder)
    {
        builder.AppendLine("#endif");
    }
    
    public static void BeginWithEditorPreproccesorBlock(this GeneratorStringBuilder builder)
    {
        builder.BeginPreproccesorBlock("WITH_EDITOR");
    }
    
    public static void BeginUnsafeBlock(this GeneratorStringBuilder builder)
    {
        builder.AppendLine("unsafe");
        builder.OpenBrace();
    }
    
    public static void EndUnsafeBlock(this GeneratorStringBuilder builder)
    {
        builder.CloseBrace();
    }
}
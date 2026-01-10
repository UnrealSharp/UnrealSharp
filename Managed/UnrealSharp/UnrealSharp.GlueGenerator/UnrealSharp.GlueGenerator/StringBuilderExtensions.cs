using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

    public static void GenerateTypeRegistration(this GeneratorStringBuilder builder, UnrealType type)
    {
        StringBuilder stringBuilder = new StringBuilder();
        using StringWriter stringWriter = new StringWriter(stringBuilder);
        
        using JsonWriter jsonWriter = new JsonTextWriter(stringWriter);
        jsonWriter.Formatting = Formatting.Indented;

        builder.StartModuleInitializer(type);

        jsonWriter.WriteStartObject();
        type.PopulateJsonObject(jsonWriter);
        jsonWriter.WriteEndObject();

        string jsonString = stringBuilder.ToString();
        builder.AppendLine($"static string JsonReflectionData => \"\"\"\n {jsonString} \n\"\"\";");
        builder.AppendLine($"static void Initialize() => RegisterManagedType(\"{type.EngineName}\", JsonReflectionData, {(byte) type.FieldType}, typeof({type.FullName}));");
        builder.CloseBrace();
    }
    
    public static void BeginType(this GeneratorStringBuilder builder, UnrealType type, string typeKeyword, string? modifiers = null, string nativeTypePtrName = SourceGenUtilities.NativeTypePtr, string overrideTypeName = "", string baseType = "", List<string>? interfaceDeclarations = null)
    {
        string protection = type.TypeAccessibility.AccessibilityToString();
        
        string declarationName = string.IsNullOrEmpty(overrideTypeName) ? type.SourceName : overrideTypeName;
        builder.AppendLine($"[GeneratedType(\"{type.EngineName}\", \"{type.FullName}\")]");
        builder.AppendLine($"{protection}partial {modifiers}{typeKeyword} {declarationName}");
        
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
        string engineName = string.IsNullOrEmpty(overrideTypeName) ? type.EngineName : overrideTypeName.Substring(1);
        builder.AppendNewBackingField($"static IntPtr {nativeTypePtrName} = UCoreUObjectExporter.CallGetType(\"{type.AssemblyName}\", \"{type.Namespace}\", \"{engineName}\");");
    }
    
    public static void BeginTypeStaticConstructor(this GeneratorStringBuilder builder, UnrealType unrealType)
    {
        BeginTypeStaticConstructor(builder, unrealType.SourceName);
    }
    
    public static void BeginTypeStaticConstructor(this GeneratorStringBuilder builder, string typeName)
    {
        builder.AppendLine();
        builder.AppendLine($"static {typeName}()");
        builder.OpenBrace();
    }
    
    public static void EndTypeStaticConstructor(this GeneratorStringBuilder builder)
    {
        builder.CloseBrace();
        builder.AppendLine();
    }

    public static void StartModuleInitializer(this GeneratorStringBuilder builder, UnrealType type)
    {
        string initializerName = $"{type.SourceName}_Initializer";
        
        builder.AppendLine();
        builder.AppendLine($"file static class {initializerName}");
        builder.OpenBrace();
        builder.AppendLine("#pragma warning disable CA2255");
        builder.AppendLine("[System.Runtime.CompilerServices.ModuleInitializer]");
        builder.AppendLine("#pragma warning restore CA2255");
        builder.AppendLine($"public static void Register() => UnrealSharp.Core.StartupJobManager.Register(typeof({initializerName}).Assembly, Initialize);");
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
    
    public static void AppendGet(this GeneratorStringBuilder builder, Accessibility accessibility)
    {
        builder.AppendLine(accessibility.AccessibilityToString());
        builder.Append("get");
    }
    
    public static void AppendSet(this GeneratorStringBuilder builder, Accessibility accessibility)
    {
        builder.AppendLine(accessibility.AccessibilityToString());
        builder.Append("set");
    }
}
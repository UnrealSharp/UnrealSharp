using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using EpicGames.Core;
using EpicGames.UHT.Parsers;
using EpicGames.UHT.Tables;
using EpicGames.UHT.Tokenizer;
using EpicGames.UHT.Types;
using EpicGames.UHT.Utils;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.Exporters;

[UnrealHeaderTool]
public static class NativeBindExporter
{
    private struct NativeBindMethod
    {
        public readonly string MethodName;
    
        public NativeBindMethod(string methodName)
        {
            MethodName = methodName;
        }
    }

    private class NativeBindTypeInfo
    {
        public readonly UhtType Type;
        public readonly List<NativeBindMethod> Methods;

        public NativeBindTypeInfo(UhtType type, List<NativeBindMethod> methods)
        {
            Type = type;
            Methods = methods;
        }
    }

    private static readonly Dictionary<UhtHeaderFile, List<NativeBindTypeInfo>> NativeBindTypes = new();

    [UhtExporter(Name = "UnrealSharpNativeGlue", 
        Description = "Exports Native Glue", 
        Options = UhtExporterOptions.Default, 
        ModuleName = "UnrealSharpCore", CppFilters = new string [] { "*.unrealsharp.gen.cpp" })]
    private static void Main(IUhtExportFactory factory)
    {
        ExportBindMethods(factory);
    }
    
    [UhtKeyword(Extends = UhtTableNames.Default, Keyword = "UNREALSHARP_FUNCTION")]
    private static UhtParseResult UNREALSHARP_FUNCTIONKeyword(UhtParsingScope topScope, UhtParsingScope actionScope, ref UhtToken token)
    {
        return ParseUnrealSharpBind(topScope, actionScope, ref token);
    }
    
    [UhtSpecifier(Extends = UhtTableNames.Function, ValueType = UhtSpecifierValueType.Legacy)]
    private static void ScriptCallableSpecifier(UhtSpecifierContext specifierContext)
    {
        UhtFunction function = (UhtFunction)specifierContext.Type;
        function.MetaData.Add("ScriptCallable", "");
    }
    
    private static UhtParseResult ParseUnrealSharpBind(UhtParsingScope topScope, UhtParsingScope actionScope, ref UhtToken token)
    {
        UhtHeaderFile headerFile = topScope.ScopeType.HeaderFile;

        topScope.TokenReader.EnableRecording();
        topScope.TokenReader
            .Require('(')
            .Require(')')
            .Require("static")
            .ConsumeUntil('(');

        int recordedTokensCount = topScope.TokenReader.RecordedTokens.Count;
        string methodName = topScope.TokenReader.RecordedTokens[recordedTokensCount - 2].Value.ToString();
        topScope.TokenReader.DisableRecording();
        
        NativeBindMethod methodInfo = new(methodName);
        
        if (!NativeBindTypes.TryGetValue(headerFile, out List<NativeBindTypeInfo>? value))
        {
            value = new List<NativeBindTypeInfo>();
            NativeBindTypes.Add(headerFile, value);
        }

        UhtType type = topScope.ScopeType;
        
        NativeBindTypeInfo? nativeBindTypeInfo = null;
        foreach (NativeBindTypeInfo bindTypeInfo in value)
        {
            if (bindTypeInfo.Type != type)
            {
                continue;
            }
            
            nativeBindTypeInfo = bindTypeInfo;
            break;
        }
        
        if (nativeBindTypeInfo == null)
        {
            nativeBindTypeInfo = new NativeBindTypeInfo(type, new List<NativeBindMethod>());
            value.Add(nativeBindTypeInfo);
        }
        
        nativeBindTypeInfo.Methods.Add(methodInfo);
        
        return UhtParseResult.Handled;
    }
    
    public static void ExportBindMethods(IUhtExportFactory factory)
    {
        foreach (KeyValuePair<UhtHeaderFile, List<NativeBindTypeInfo>> bindMethod in NativeBindTypes)
        {
            UhtHeaderFile headerFile = bindMethod.Key;
            List<NativeBindTypeInfo> containingTypesInHeader = bindMethod.Value;
            
            GeneratorStringBuilder builder = new();
            builder.AppendLine("#include \"UnrealSharpBinds.h\"");
            builder.AppendLine($"#include \"{headerFile.FilePath}\"");
            builder.AppendLine();
            
            foreach (NativeBindTypeInfo containingType in containingTypesInHeader)
            {
                UhtType topType = containingType.Type;
                List<NativeBindMethod> methods = containingType.Methods;
                
                string typeName = $"Z_Construct_U{topType.EngineClassName}_UnrealSharp_Binds_" + topType.SourceName;
                builder.Append($"struct {typeName}");
            
                builder.OpenBrace();
            
                foreach (NativeBindMethod method in methods)
                {
                    builder.AppendLine($"static const FCSExportedFunction UnrealSharpBind_{method.MethodName};");
                }
            
                builder.CloseBrace();
                builder.Append(";");
            
                foreach (NativeBindMethod method in methods)
                {
                    string functionReference = $"{topType.SourceName}::{method.MethodName}";
                    builder.AppendLine($"const FCSExportedFunction {typeName}::UnrealSharpBind_{method.MethodName}");
                    builder.Append($" = FCSExportedFunction(\"{topType.EngineName}\", \"{method.MethodName}\", (void*)&{functionReference}, GetFunctionSize({functionReference}));");
                }
                
                builder.AppendLine();
                builder.AppendLine();
            }

            UHTManifest.Module manifestModule;
            #if UE_5_5_OR_LATER
            manifestModule = headerFile.Module.Module;
            #else
            manifestModule= headerFile.Package.GetModule();
            #endif
            
            string outputDirectory = manifestModule.OutputDirectory;
            string fileName = headerFile.FileNameWithoutExtension + ".unrealsharp.gen.cpp";
            string filePath = Path.Combine(outputDirectory, fileName);
            
            factory.CommitOutput(filePath, builder.ToString());
        }
    }
}
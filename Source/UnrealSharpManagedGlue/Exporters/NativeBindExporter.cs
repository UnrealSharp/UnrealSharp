using System;
using System.Collections.Generic;
using EpicGames.Core;
using EpicGames.UHT.Parsers;
using EpicGames.UHT.Tables;
using EpicGames.UHT.Tokenizer;
using EpicGames.UHT.Types;
using EpicGames.UHT.Utils;

namespace UnrealSharpScriptGenerator.Exporters;

public struct CSBindMethod
{
    public string MethodName;
    
    public CSBindMethod(string methodName)
    {
        MethodName = methodName;
    }
}

[UnrealHeaderTool]
public static class NativeBindExporter
{
    static Dictionary<UhtType, List<CSBindMethod>> BindMethods = new();

    [UhtExporter(Name = "UnrealSharpNativeGlue", 
        Description = "Exports Native Glue", 
        Options = UhtExporterOptions.Default, 
        ModuleName = "UnrealSharpCore", 
        CppFilters = ["*.unrealsharp.gen.cpp"])]
    private static void Main(IUhtExportFactory factory)
    {
        ExportBindMethods(factory);
    }
    
    [UhtKeyword(Extends = UhtTableNames.Default, Keyword = "UNREALSHARP_FUNCTION")]
    private static UhtParseResult UNREALSHARP_FUNCTIONKeyword(UhtParsingScope topScope, UhtParsingScope actionScope, ref UhtToken token)
    {
        return ParseUnrealSharpBind(topScope, actionScope, ref token);
    }
    
    private static UhtParseResult ParseUnrealSharpBind(UhtParsingScope topScope, UhtParsingScope actionScope, ref UhtToken token)
    {
        UhtType topScopeType = topScope.ScopeType;

        topScope.TokenReader.EnableRecording();
        topScope.TokenReader
            .Require('(')
            .Require(')')
            .Require("static")
            .ConsumeUntil('(');

        int recordedTokensCount = topScope.TokenReader.RecordedTokens.Count;
        string methodName = topScope.TokenReader.RecordedTokens[recordedTokensCount - 2].Value.ToString();
        topScope.TokenReader.DisableRecording();
        
        CSBindMethod methodInfo = new(methodName);
        
        if (!BindMethods.TryGetValue(topScopeType, out List<CSBindMethod>? value))
        {
            value = new List<CSBindMethod>();
            BindMethods.Add(topScopeType, value);
        }

        value.Add(methodInfo);
        
        return UhtParseResult.Handled;
    }
    
    public static void ExportBindMethods(IUhtExportFactory factory)
    {
        foreach (KeyValuePair<UhtType, List<CSBindMethod>> bindMethod in BindMethods)
        {
            UhtType type = bindMethod.Key;
            List<CSBindMethod> methods = bindMethod.Value;
            
            GeneratorStringBuilder builder = new();
            builder.AppendLine("#include \"UnrealSharpBinds.h\"");
            builder.AppendLine($"#include \"{type.HeaderFile}\"");
            builder.AppendLine();
            
            string typeName = $"Z_Construct_U{type.EngineClassName}_UnrealSharp_Binds_" + type.SourceName;
            builder.Append($"struct {typeName}");
            
            builder.OpenBrace();
            
            foreach (CSBindMethod method in methods)
            {
                builder.AppendLine($"static const FCSExportedFunction UnrealSharpBind_{method.MethodName};");
            }
            
            builder.CloseBrace();
            builder.Append(";");
            
            foreach (CSBindMethod method in methods)
            {
                string functionReference = $"{type.SourceName}::{method.MethodName}";
                builder.AppendLine($"const FCSExportedFunction {typeName}::UnrealSharpBind_{method.MethodName}");
                builder.Append($" = FCSExportedFunction(\"{type.EngineName}\", \"{method.MethodName}\", &{functionReference}, GetFunctionSize({functionReference}));");
            }
            
            string filePath = factory.MakePath(type.HeaderFile, ".unrealsharp.gen.cpp");
            factory.CommitOutput(filePath, builder.ToString());
        }
    }
}
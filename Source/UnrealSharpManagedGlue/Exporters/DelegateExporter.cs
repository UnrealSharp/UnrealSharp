using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.PropertyTranslators;
using UnrealSharpManagedGlue.SourceGeneration;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue.Exporters;

public static class DelegateExporter
{
    public static void ExportDelegate(UhtFunction function)
    {
        string delegateName = DelegateBasePropertyTranslator.GetDelegateName(function);
        
        GeneratorStringBuilder stringBuilder = new();
        
        stringBuilder.StartGlueFile(function);
        stringBuilder.AppendLine();
        
        string superClass;
        if (function.HasAllFlags(EFunctionFlags.MulticastDelegate))
        {
            superClass = $"MulticastDelegate<{delegateName}>";
        }
        else
        {
            superClass = $"Delegate<{delegateName}>";
        }
        
        FunctionExporter functionExporter = FunctionExporter.ExportDelegateSignature(stringBuilder, function, delegateName);
        string wrapperName = DelegateBasePropertyTranslator.GetWrapperName(function);
        
        stringBuilder.DeclareType(function, "class", wrapperName, superClass);
        
        FunctionExporter.ExportDelegateGlue(stringBuilder, functionExporter, delegateName);
        
        stringBuilder.AppendLine($"static {wrapperName}()");
        stringBuilder.OpenBrace();
        ExportDelegateFunctionStaticConstruction(stringBuilder, function, wrapperName);
        stringBuilder.CloseBrace();
        
        stringBuilder.CloseBrace();
        
        FunctionExporter.ExportDelegateExtensions(stringBuilder, functionExporter, superClass);
        
        // Use modified delegate name (with Outer prefix) as file name to prevent same-named delegates from overwriting each other
        string directory = FileExporter.GetDirectoryPath(function.Package);

        stringBuilder.EndGlueFile(function);
        FileExporter.SaveGlueToDisk(function.Package, directory, delegateName, stringBuilder.ToString());
    }

    private static void ExportDelegateFunctionStaticConstruction(GeneratorStringBuilder builder, UhtFunction function, string wrapperName)
    {
        string delegateName = function.SourceName;
        builder.AppendLine($"{delegateName}_NativeFunction = {ExporterCallbacks.CoreUObjectCallbacks}.CallGetType({NameMapper.ExportGetAssemblyName(wrapperName)}, \"{function.GetNamespace()}\", \"{function.EngineName}\");");
        if (function.HasParameters)
        {
            builder.AppendLine($"{delegateName}_ParamsSize = {ExporterCallbacks.UFunctionCallbacks}.CallGetNativeFunctionParamsSize({delegateName}_NativeFunction);");
        }
        
        foreach (UhtProperty parameter in function.Properties)
        {
            PropertyTranslator propertyTranslator = parameter.GetTranslator()!;
            propertyTranslator.ExportParameterStaticConstructor(builder, parameter, function, parameter.SourceName, delegateName);
        }
    }
}
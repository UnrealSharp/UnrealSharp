using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.Exporters;
using UnrealSharpManagedGlue.Model;
using UnrealSharpManagedGlue.PropertyTranslators;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue;

public static class CSharpExporter
{
    private const string SpecialTypesJson = "SpecialTypes.json";
    public static bool HasModifiedEngineGlue;
    
    public static void StartExport()
    {
        if (HasChangedGeneratorSourceRecently())
        {
            Console.WriteLine("Detected changes in generator source, re-exporting entire API...");
            FileExporter.CleanModuleFolders();
        }
        else
        {
            PackageHeadersTracker.DeserializeModuleHeaders();
        }

        Console.WriteLine("Starting C# export of Unreal Engine API...");

        #if UE_5_5_OR_LATER
        foreach (UhtModule module in GeneratorStatics.Factory.Session.Modules)
        {
            foreach (UhtPackage modulePackage in module.Packages)
            {
                ExportPackage(modulePackage);
            }
        }
        #else
        foreach (UhtPackage package in Program.Factory.Session.Packages)
        {
            ExportPackage(package);
        }
        #endif
        
        PreprocessorExporter.StartExportingPreprocessors();
        PackageHeadersTracker.SerializeModuleData();
        OutputTypeRules();
        
        TaskManager.WaitForTasks();
        
        FunctionExporter.StartExportingExtensionMethods();
        AutocastExporter.StartExportingAutocastFunctions();
        
        TaskManager.WaitForTasks();
    }

    static bool HasChangedGeneratorSourceRecently()
    {
        string executingAssemblyPath = Assembly.GetExecutingAssembly().Location;
        DateTime executingAssemblyLastWriteTime = File.GetLastWriteTimeUtc(executingAssemblyPath);

        string generatedCodeDirectory = GeneratorStatics.PluginModule.OutputDirectory;
        string timestampFilePath = Path.Combine(generatedCodeDirectory, "Timestamp");
        string typeInfoFilePath = Path.Combine(generatedCodeDirectory, SpecialTypesJson);

        if (!File.Exists(timestampFilePath) || !File.Exists(typeInfoFilePath) || !Directory.Exists(GeneratorStatics.EngineGluePath))
        {
            return true;
        }

        if (TypeRulesChanged(typeInfoFilePath))
        {
            return true;
        }

        DateTime savedTimestampUtc = File.GetLastWriteTimeUtc(timestampFilePath);
        return executingAssemblyLastWriteTime > savedTimestampUtc;
    }

    static bool TypeRulesChanged(string typeInfoFilePath)
    {
        using FileStream fileStream = new FileStream(typeInfoFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        
        SpecialTypeInfo? rules = JsonSerializer.Deserialize<SpecialTypeInfo>(fileStream);
        if (rules == null)
        {
            return true;
        }

        return !rules.Equals(PropertyTranslatorManager.SpecialTypeInfo);
    }

    static void OutputTypeRules()
    {
        string generatedCodeDirectory = GeneratorStatics.PluginModule.OutputDirectory;
        string typeInfoFilePath = Path.Combine(generatedCodeDirectory, SpecialTypesJson);
        
        using var fileStream = new FileStream(typeInfoFilePath, FileMode.Create, FileAccess.Write);
        JsonSerializer.Serialize(fileStream, PropertyTranslatorManager.SpecialTypeInfo);
    }

    private static void ExportPackage(UhtPackage package)
    {
        if (!package.ShouldExportPackage())
        {
            return;
        }

        if (!package.IsPartOfEngine())
        {
            package.FindOrAddProjectInfo();
        }

        string packageName = package.GetShortName();
        string generatedPath = FileExporter.GetDirectoryPath(package);
        bool generatedGlueFolderExists = Directory.Exists(generatedPath);

        UhtHeaderFile[] processedHeaders = new UhtHeaderFile[package.Children.Count];
        
        for (int i = 0; i < package.Children.Count; i++)
        {
            UhtType child = package.Children[i];
            
            if (!generatedGlueFolderExists || PackageHeadersTracker.HasHeaderFileChanged(packageName, child.HeaderFile))
            {
                ForEachChild(child, ExportType);
            }
            else
            {
                ForEachChild(child, FileExporter.AddUnchangedType);
            }
            
            processedHeaders[i] = child.HeaderFile;
        }
        
        PackageHeadersTracker.RecordPackageHeadersWriteTime(packageName, processedHeaders);
    }

    public static void ForEachChild(UhtType child, Action<UhtType> action)
    {
        #if UE_5_5_OR_LATER
        action(child);

        foreach (UhtType type in child.Children)
        {
            action(type);
        }
        #else
        foreach (UhtType type in child.Children)
        {
            action(type);

            foreach (UhtType innerType in type.Children)
            {
                action(innerType);
            }
        }
        #endif
    }
    
    public static void ForEachChildRecursive(UhtType child, Action<UhtType> action)
    {
        action(child);

        foreach (UhtType type in child.Children)
        {
            ForEachChildRecursive(type, action);
        }
    }

    private static void ExportType(UhtType type)
    {
        if (type.HasMetadata(PackageUtilities.SkipGlueGenerationDefine) || PropertyTranslatorManager.SpecialTypeInfo.Structs.SkippedTypes.Contains(type.SourceName))
        {
            return;
        }

        bool isManualExport = PropertyTranslatorManager.SpecialTypeInfo.Structs.BlittableTypes.ContainsKey(type.SourceName);

        if (type is UhtClass classObj)
        {
            if (classObj.HasAllFlags(EClassFlags.Interface))
            {
                if (isManualExport)
                {
                    return;
                }

                if (classObj.ClassType is not UhtClassType.Interface && type != GeneratorStatics.Factory.Session.IInterface)
                {
                    return;
                }

                TaskManager.StartTask(_ => { InterfaceExporter.ExportInterface(classObj); });
            }
            else
            {
                TaskManager.StartTask(_ => { ClassExporter.ExportClass(classObj, isManualExport); });
            }
        }
        else if (type is UhtEnum enumObj)
        {
            if (isManualExport)
            {
                return;
            }

            TaskManager.StartTask(_ => { EnumExporter.ExportEnum(enumObj); });
        }
        else if (type is UhtScriptStruct structObj)
        {
            isManualExport = PropertyTranslatorManager.SpecialTypeInfo.Structs.BlittableTypes.TryGetValue(structObj.SourceName, out var info) && info.ManagedType is not null;
            TaskManager.StartTask(_ => { StructExporter.ExportStruct(structObj, isManualExport); });
        }
        else if (type.EngineType is UhtEngineType.Delegate 
                #if UE_5_7_OR_LATER
                 or UhtEngineType.SparseDelegate
                #endif
                 )
        {
            if (isManualExport)
            {
                return;
            }

            UhtFunction delegateFunction = (UhtFunction) type;
            if (!ScriptGeneratorUtilities.CanExportParameters(delegateFunction) || delegateFunction.ReturnProperty != null)
            {
                return;
            }
            
            TaskManager.StartTask(_ => { DelegateExporter.ExportDelegate(delegateFunction); });
        }
    }
}

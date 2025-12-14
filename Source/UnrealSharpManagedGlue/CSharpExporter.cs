using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using EpicGames.Core;
using EpicGames.UHT.Types;
using EpicGames.UHT.Utils;
using UnrealSharpScriptGenerator.Exporters;
using UnrealSharpScriptGenerator.Model;
using UnrealSharpScriptGenerator.PropertyTranslators;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator;

public static class CSharpExporter
{
    private const string SpecialtypesJson = "SpecialTypes.json";
    public static bool HasModifiedEngineGlue;

    private static readonly List<Task> Tasks = new();
    private static readonly List<string> ExportedDelegates = new();

    public static void StartExport()
    {
        if (HasChangedGeneratorSourceRecently())
        {
            Console.WriteLine("Detected changes in generator source, re-exporting entire API...");
            FileExporter.CleanModuleFolders();
        }
        else
        {
            ModuleHeadersTracker.DeserializeModuleHeaders();
        }

        Console.WriteLine("Starting C# export of Unreal Engine API...");

        #if UE_5_5_OR_LATER
        foreach (UhtModule module in Program.Factory.Session.Modules)
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

        WaitForTasks();

        FunctionExporter.StartExportingExtensionMethods(Tasks);

        WaitForTasks();

        AutocastExporter.StartExportingAutocastFunctions(Tasks);

        WaitForTasks();

        Console.WriteLine("Exporting UE5 defines");
        PreprocessorExporter.StartExportingPreprocessors(Program.Factory.Session.EngineDirectory, Tasks);

        WaitForTasks();

        ModuleHeadersTracker.SerializeModuleData();

        string generatedCodeDirectory = Program.PluginModule.OutputDirectory;
        string typeInfoFilePath = Path.Combine(generatedCodeDirectory, SpecialtypesJson);
        OutputTypeRules(typeInfoFilePath);
    }

    static bool HasChangedGeneratorSourceRecently()
    {
        string executingAssemblyPath = Assembly.GetExecutingAssembly().Location;
        DateTime executingAssemblyLastWriteTime = File.GetLastWriteTimeUtc(executingAssemblyPath);

        string generatedCodeDirectory = Program.PluginModule.OutputDirectory;
        string timestampFilePath = Path.Combine(generatedCodeDirectory, "Timestamp");
        string typeInfoFilePath = Path.Combine(generatedCodeDirectory, SpecialtypesJson);

        if (!File.Exists(timestampFilePath) || !File.Exists(typeInfoFilePath) || !Directory.Exists(Program.EngineGluePath))
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
        using var fs = new FileStream(typeInfoFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var rules = JsonSerializer.Deserialize<SpecialTypeInfo>(fs);
        if (rules == null)
        {
            return true;
        }

        return !rules.Equals(PropertyTranslatorManager.SpecialTypeInfo);
    }

    static void OutputTypeRules(string typeInfoFilePath)
    {
        using var fs = new FileStream(typeInfoFilePath, FileMode.Create, FileAccess.Write);
        JsonSerializer.Serialize(fs, PropertyTranslatorManager.SpecialTypeInfo);
    }

    private static void WaitForTasks()
    {
        Task[] waitTasks = Tasks.ToArray();
        if (waitTasks.Length > 0)
        {
            Task.WaitAll(waitTasks);
        }
        Tasks.Clear();
    }

    private static void ExportPackage(UhtPackage package)
    {
        if (!package.ShouldExport())
        {
            return;
        }

        if (!Program.BuildingEditor && package.PackageFlags.HasAnyFlags(EPackageFlags.EditorOnly | EPackageFlags.UncookedOnly))
        {
            return;
        }

        if (!package.IsPartOfEngine())
        {
            package.FindOrAddProjectInfo();
        }

        string packageName = package.GetShortName();
        string generatedPath = FileExporter.GetDirectoryPath(package);
        bool doesDirectoryExist = Directory.Exists(generatedPath);

        List<UhtHeaderFile> processedHeaders = new List<UhtHeaderFile>(package.Children.Count);
        
        foreach (UhtType child in package.Children)
        {
            if (!doesDirectoryExist || ModuleHeadersTracker.HasHeaderChanged(packageName, child.HeaderFile))
            {
                if (ModuleHeadersTracker.HasDataFromDisk)
                {
                    string headerFileName = Path.GetFileName(child.HeaderFile.FilePath);
                    Console.WriteLine($"Detected changes in header file: {headerFileName}, re-exporting...");
                }
                
                ForEachChild(child, ExportType);
            }
            else
            {
                ForEachChild(child, FileExporter.AddUnchangedType);
            }
            
            processedHeaders.Add(child.HeaderFile);
        }
        
        ModuleHeadersTracker.RecordHeadersWriteTime(packageName, processedHeaders);
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

                if (classObj.ClassType is not UhtClassType.Interface && type != Program.Factory.Session.IInterface)
                {
                    return;
                }

                Tasks.Add(Program.Factory.CreateTask(_ => { InterfaceExporter.ExportInterface(classObj); })!);
            }
            else
            {
                Tasks.Add(Program.Factory.CreateTask(_ => { ClassExporter.ExportClass(classObj, isManualExport); })!);
            }
        }
        else if (type is UhtEnum enumObj)
        {
            if (isManualExport)
            {
                return;
            }

            Tasks.Add(Program.Factory.CreateTask(_ => { EnumExporter.ExportEnum(enumObj); })!);
        }
        else if (type is UhtScriptStruct structObj)
        {
            isManualExport = PropertyTranslatorManager.SpecialTypeInfo.Structs.BlittableTypes.TryGetValue(structObj.SourceName, out var info) && info.ManagedType is not null;
            Tasks.Add(Program.Factory.CreateTask(_ => { StructExporter.ExportStruct(structObj, isManualExport); })!);
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

            // Use full path (including Outer) as unique identifier to distinguish delegates with same name but different owner classes
            // e.g., UComboBoxString::OnSelectionChangedEvent vs UComboBoxKey::OnSelectionChangedEvent
            string outerName = delegateFunction.Outer != null ? delegateFunction.Outer.SourceName : "";
            string delegateName = DelegateBasePropertyTranslator.GetFullDelegateName(delegateFunction);
            string uniqueDelegateKey = !string.IsNullOrEmpty(outerName) 
                ? $"{outerName}.{delegateName}" 
                : delegateName;
            
            if (ExportedDelegates.Contains(uniqueDelegateKey))
            {
                return;
            }

            ExportedDelegates.Add(uniqueDelegateKey);
            Tasks.Add(Program.Factory.CreateTask(_ => { DelegateExporter.ExportDelegate(delegateFunction); })!);
        }
    }
}

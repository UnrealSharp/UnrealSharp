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

class ModuleFolders
{
    public Dictionary<string, DateTime> DirectoryToWriteTime { get; set; } = new();
    public bool HasBeenExported;
}

public static class CSharpExporter
{
    const string ModuleDataFileName = "UnrealSharpModuleData.json";
    private const string SpecialtypesJson = "SpecialTypes.json";
    public static bool HasModifiedEngineGlue;
    
    private static readonly List<Task> Tasks = new();
    private static readonly List<string> ExportedDelegates = new();
    private static readonly Dictionary<string, DateTime> CachedDirectoryTimes = new();
    private static Dictionary<string, ModuleFolders?> _modulesWriteInfo = new();
    private static readonly HashSet<string> PluginDirs = [];
    
    public static void StartExport()
    {
        if (!HasChangedGeneratorSourceRecently())
        {
            // The source for this generator hasn't changed, so we don't need to re-export the whole API.
            DeserializeModuleData();
        }
        else
        {
            // Just in case the source has changed, we need to clean the old files
            Console.WriteLine("Managed Glue Generator has changed its source, cleaning old files...");
            FileExporter.CleanModuleFolders();
        }
        
        Console.WriteLine("Exporting C++ to C#...");
        
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
        
        SerializeModuleData();
        
        string generatedCodeDirectory = Program.PluginModule.OutputDirectory;
        string typeInfoFilePath = Path.Combine(generatedCodeDirectory, SpecialtypesJson);
        OutputTypeRules(typeInfoFilePath);
    }
    
    static void DeserializeModuleData()
    {
        if (!Directory.Exists(Program.EngineGluePath) || !Directory.Exists(Program.ProjectGluePath))
        {
            return;
        }
        
        string outputPath = Path.Combine(Program.PluginModule.OutputDirectory, ModuleDataFileName);
        
        if (!File.Exists(outputPath))
        {
            return;
        }
        
        using FileStream fileStream = new FileStream(outputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        Dictionary<string, ModuleFolders>? jsonValue = JsonSerializer.Deserialize<Dictionary<string, ModuleFolders>>(fileStream);

        if (jsonValue != null)
        {
            _modulesWriteInfo = new Dictionary<string, ModuleFolders?>(jsonValue!);
        }
    }
	
    static void SerializeModuleData()
    {
        string outputPath = Path.Combine(Program.PluginModule.OutputDirectory, ModuleDataFileName);
        using FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        JsonSerializer.Serialize(fs, _modulesWriteInfo);
    }

    static bool HasChangedGeneratorSourceRecently()
    {
        string executingAssemblyPath = Assembly.GetExecutingAssembly().Location;
        DateTime executingAssemblyLastWriteTime = File.GetLastWriteTimeUtc(executingAssemblyPath);
        
        string generatedCodeDirectory = Program.PluginModule.OutputDirectory;
        string timestampFilePath = Path.Combine(generatedCodeDirectory, "Timestamp");
        string typeInfoFilePath = Path.Combine(generatedCodeDirectory, SpecialtypesJson);
        
        if (!File.Exists(timestampFilePath) || !File.Exists(typeInfoFilePath) || !Directory.Exists(Program.EngineGluePath) || !Directory.Exists(Program.ProjectGluePath))
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

        string packageName = package.GetShortName();
    
        if (!_modulesWriteInfo.TryGetValue(packageName, out ModuleFolders? lastEditTime))
        {
            lastEditTime = new ModuleFolders();
            _modulesWriteInfo.Add(packageName, lastEditTime);
        }
    
        HashSet<string> processedDirectories = new();
        
        string generatedPath = FileExporter.GetDirectoryPath(package);
        bool doesDirectoryExist = Directory.Exists(generatedPath);
        
        foreach (UhtType child in package.Children)
        {
            string directoryName = Path.GetDirectoryName(child.HeaderFile.FilePath)!;
            
            // We only need to export the C++ directory if it doesn't exist or if it has been modified
            if (!doesDirectoryExist || ShouldExportDirectory(directoryName, lastEditTime!))
            {
                processedDirectories.Add(directoryName);
                ForEachChild(child, ExportType);
            }
            else
            {
                ForEachChild(child, FileExporter.AddUnchangedType);
            }
        }
        
        if (processedDirectories.Count == 0)
        {
            // No directories in this package have been exported or modified
            return;
        }
        
        // The glue has been exported, so we need to update the last write times
        UpdateLastWriteTimes(processedDirectories, lastEditTime!);
    }
    
    private static void ForEachChild(UhtType child, Action<UhtType> action)
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

    public static bool HasBeenExported(string directory)
    {
        return _modulesWriteInfo.TryGetValue(directory, out ModuleFolders? lastEditTime) && lastEditTime is
        {
            HasBeenExported: true
        };
    }

    private static bool ShouldExportDirectory(string directoryPath, ModuleFolders lastEditTime)
    {
        if (!CachedDirectoryTimes.TryGetValue(directoryPath, out DateTime cachedTime))
        {
            DateTime currentWriteTime = Directory.GetLastWriteTimeUtc(directoryPath);
            CachedDirectoryTimes[directoryPath] = currentWriteTime;
            cachedTime = currentWriteTime;
        }
        
        return !lastEditTime.DirectoryToWriteTime.TryGetValue(directoryPath, out DateTime lastEditTimeValue) || lastEditTimeValue != cachedTime;
    }

    private static void UpdateLastWriteTimes(HashSet<string> directories, ModuleFolders lastEditTime)
    {
        foreach (string directory in directories)
        {
            if (!CachedDirectoryTimes.TryGetValue(directory, out DateTime cachedTime))
            {
                continue;
            }
            
            lastEditTime.DirectoryToWriteTime[directory] = cachedTime;
            lastEditTime.HasBeenExported = true;
        }
    }
    
    private static void ExportType(UhtType type)
    {
        if (type.HasMetadata(PackageUtilities.SkipGlueGenerationDefine))
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
        else if (type.EngineType == UhtEngineType.Delegate)
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
            
            // There are some duplicate delegates in the same modules, so we need to check if we already exported it
            string delegateName = DelegateBasePropertyTranslator.GetFullDelegateName(delegateFunction);
            if (ExportedDelegates.Contains(delegateName))
            {
                return;
            }
            
            ExportedDelegates.Add(delegateName);
            Tasks.Add(Program.Factory.CreateTask(_ => { DelegateExporter.ExportDelegate(delegateFunction); })!);
        }
    }
}
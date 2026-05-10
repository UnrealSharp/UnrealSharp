using System;
using System.Collections.Generic;
using System.IO;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.Model;
using UnrealSharpManagedGlue.PropertyTranslators;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue.Exporters;

public static class PackageExporter
{
    public static void ExportPackages()
    {
        IEnumerable<ModuleInfo> modules = ModuleUtilities.Modules;
        
        foreach (ModuleInfo moduleInfo in modules)
        {
            ExportPackage(moduleInfo.Module);
        }
        
        TaskManager.WaitForTasks();
    }
    
    private static void ExportPackage(UhtPackage package)
    {
        string packageName = package.GetModuleShortName();
        string generatedPath = package.GetModuleUhtOutputDirectory();
        bool generatedGlueFolderExists = Directory.Exists(generatedPath);

        int childrenCount = package.Children.Count;
        UhtHeaderFile[] processedHeaders = new UhtHeaderFile[childrenCount];
        
        for (int i = 0; i < childrenCount; i++)
        {
            UhtType child = package.Children[i];
            bool hasChanged = !generatedGlueFolderExists || PackageHeadersTracker.HasHeaderFileChanged(packageName, child.HeaderFile);
            Action<UhtType> processAction = hasChanged ? ExportType : FileExporter.AddUnchangedType;

            processAction(child);

            foreach (UhtType type in child.Children)
            {
                processAction(type);
            }
            
            processedHeaders[i] = child.HeaderFile;
        }
        
        PackageHeadersTracker.RecordPackageHeadersWriteTime(packageName, processedHeaders);
    }

    private static void ExportType(UhtType type)
    {
        SpecialStructInfo specialStructs = PropertyTranslatorManager.SpecialTypeInfo.Structs;

        if (type.CanSkipType() || specialStructs.SkippedTypes.Contains(type.SourceName))
        {
            return;
        }

        bool isManualExport = specialStructs.BlittableTypes.ContainsKey(type.SourceName);
        Action? exportAction = null;

        if (type is UhtClass classObj)
        {
            if (classObj.HasAllFlags(EClassFlags.Interface))
            {
                if (classObj.ClassType is UhtClassType.Interface || type == GeneratorStatics.Factory.Session.IInterface)
                {
                    exportAction = () => InterfaceExporter.ExportInterface(classObj);
                }
            }
            else
            {
                exportAction = () => ClassExporter.ExportClass(classObj, isManualExport);
            }
        }
        else if (type is UhtEnum enumObj)
        {
            if (isManualExport)
            {
                return;
            }
            
            exportAction = () => EnumExporter.ExportEnum(enumObj);
        }
        else if (type is UhtScriptStruct structObj)
        {
            bool isBlittable = specialStructs.BlittableTypes.TryGetValue(structObj.SourceName, out BlittableStructInfo info) && info.ManagedType is not null;
            exportAction = () => StructExporter.ExportStruct(structObj, isBlittable);
        }
        else if (IsDelegateType(type))
        {
            UhtFunction delegateFunction = (UhtFunction)type;
            if (!isManualExport && delegateFunction.ReturnProperty == null && delegateFunction.CanExportParameters())
            {
                exportAction = () => DelegateExporter.ExportDelegate(delegateFunction);
            }
        }

        if (exportAction != null)
        {
            TaskManager.StartTask(_ => exportAction());
        }
    }

    private static bool IsDelegateType(UhtType type)
    {
        return type.EngineType == UhtEngineType.Delegate 
#if UE_5_7_OR_LATER
               || type.EngineType == UhtEngineType.SparseDelegate
#endif
               ;
    }
}
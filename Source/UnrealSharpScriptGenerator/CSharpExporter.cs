using System.Collections.Generic;
using System.Threading.Tasks;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Exporters;
using UnrealSharpScriptGenerator.PropertyTranslators;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator;

public static class CSharpExporter
{
    private static readonly List<Task> Tasks = new();
    private static readonly List<string> ExportedDelegates = new();
    
    public static void StartExport()
    {
        Tasks.Add(Program.Factory.CreateTask(_ => { ClassExporter.ExportClass(Program.Factory.Session.UObject); })!);
        Tasks.Add(Program.Factory.CreateTask(_ => { InterfaceExporter.ExportInterface(Program.Factory.Session.UInterface); })!);
        
        foreach (UhtPackage package in Program.Factory.Session.Packages)
        {
            ExportPackage(package);
        }
        
        WaitForTasks();
        
        FunctionExporter.StartExportingExtensionMethods(Tasks);
        
        WaitForTasks();
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
        if (!Program.BuildingEditor && package.PackageFlags.HasAnyFlags(EPackageFlags.EditorOnly | EPackageFlags.UncookedOnly))
        {
            return;
        }
        
        foreach (UhtType child in package.Children)
        {
            foreach (UhtType type in child.Children)
            {
                // Finds classes, enums, structs, and delegates in the header file
                ExportType(type);
                
                foreach (UhtType innerType in type.Children)
                {
                    // Finds nested classes, enums, structs, and delegates
                    ExportType(innerType);
                }
            }
        }
    }

    private static void ExportType(UhtType type)
    {
        if (type.HasMetadata("NotGeneratorValid") || PropertyTranslatorManager.ManuallyExportedTypes.Contains(type.SourceName))
        {
            return;
        }
        
        if (type is UhtClass classObj)
        {
            if (classObj.HasAllFlags(EClassFlags.Interface))
            {
                if (classObj.ClassType is not UhtClassType.Interface)
                {
                    return;
                }
                
                Tasks.Add(Program.Factory.CreateTask(_ => { InterfaceExporter.ExportInterface(classObj); })!);
            }
            else
            {
                Tasks.Add(Program.Factory.CreateTask(_ => { ClassExporter.ExportClass(classObj); })!);
            }
        }
        else if (type is UhtEnum enumObj)
        {
            Tasks.Add(Program.Factory.CreateTask(_ => { EnumExporter.ExportEnum(enumObj); })!);
        }
        else if (type is UhtScriptStruct structObj)
        {
            Tasks.Add(Program.Factory.CreateTask(_ => { StructExporter.ExportStruct(structObj); })!);
        }
        else if (type.EngineType == UhtEngineType.Delegate)
        {
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
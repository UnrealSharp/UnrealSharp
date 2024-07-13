using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Exporters;
using UnrealSharpScriptGenerator.PropertyTranslators;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator;

public static class CSharpExporter
{
    private static readonly List<Task> Tasks = new();
    
    public static void StartExport()
    {
        Tasks.Add(Program.Factory.CreateTask(_ => { ClassExporter.ExportClass(Program.Factory.Session.UObject); })!);
        
        foreach (UhtPackage package in Program.Factory.Session.Packages)
        {
            ProcessPackage(package);
        }
        
        WaitForTasks();
        
        // These are only populated once all classes have been exported
        foreach (UhtPackage package in Program.Factory.Session.Packages)
        {
            FunctionExporter.StartExportingExtensionMethods(package, Tasks);
        }
        
        WaitForTasks();
    }
    
    private static void WaitForTasks()
    {
        Task[] waitTasks = Tasks.ToArray();
        if (waitTasks.Length > 0)
        {
            Task.WaitAll(waitTasks);
        }
    }

    private static void ProcessPackage(UhtPackage package)
    {
        foreach (UhtType packageChild in package.Children)
        {
            ExportType(packageChild);
            
            foreach (UhtType type in packageChild.Children)
            {
                ExportType(type);
            }
        }
    }

    private static void ExportType(UhtType type)
    {
        if (type.HasMetadata("NotGeneratorValid") || PropertyTranslatorManager.ManuallyExportedTypes.Contains(type.EngineName))
        {
            return;
        }
        
        if (type is UhtClass classObj)
        {
            if (classObj.ClassType is UhtClassType.Interface or UhtClassType.NativeInterface)
            {
                if (classObj.AlternateObject is not UhtClass alternateClass)
                {
                    return;
                }
                
                Tasks.Add(Program.Factory.CreateTask(_ => { InterfaceExporter.ExportInterface(alternateClass); }));
            }
            else
            {
                Tasks.Add(Program.Factory.CreateTask(_ => { ClassExporter.ExportClass(classObj); }));
            }
        }
        else if (type is UhtEnum enumObj)
        {
            Tasks.Add(Program.Factory.CreateTask(_ => { EnumExporter.ExportEnum(enumObj); }));
        }
        else if (type is UhtScriptStruct structObj)
        {
            Tasks.Add(Program.Factory.CreateTask(_ => { StructExporter.ExportStruct(structObj); }));
        }
    }
}
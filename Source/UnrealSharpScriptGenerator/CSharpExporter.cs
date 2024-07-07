using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Exporters;
using UnrealSharpScriptGenerator.PropertyTranslators;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator;

public class CSharpExporter
{
    private readonly List<Task?> _tasks = new();
    
    public void StartExport()
    {
        ExportType(Program.Factory.Session.UObject);
        
        foreach (UhtPackage package in Program.Factory.Session.Packages)
        {
            ProcessPackage(package);
        }
        
        Task[] waitTasks = _tasks.Where(x => x != null).Cast<Task>().ToArray();
        if (waitTasks.Length > 0)
        {
            Task.WaitAll(waitTasks);
        }
    }
    
    private void ProcessPackage(UhtType package)
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

    private void ExportType(UhtType type)
    {
        if (type.HasMetadata("NotGeneratorValid") || PropertyTranslatorManager.ManuallyExportedTypes.Contains(type.EngineName))
        {
            return;
        }
        
        if (type is UhtClass classObj)
        {
            if (classObj.ClassType == UhtClassType.Interface)
            {
                _tasks.Add(Program.Factory.CreateTask(_ => { InterfaceExporter.ExportInterface(classObj); }));
            }
            _tasks.Add(Program.Factory.CreateTask(_ => { ClassExporter.ExportClass(classObj); }));
        }
        else if (type is UhtEnum enumObj)
        {
            _tasks.Add(Program.Factory.CreateTask(_ => { EnumExporter.ExportEnum(enumObj); }));
        }
        else if (type is UhtScriptStruct structObj)
        {
            _tasks.Add(Program.Factory.CreateTask(_ => { StructExporter.ExportStruct(structObj); }));
        }
    }
}
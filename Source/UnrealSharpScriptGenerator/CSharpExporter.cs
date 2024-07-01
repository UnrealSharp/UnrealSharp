using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EpicGames.UHT.Types;
using EpicGames.UHT.Utils;
using UnrealSharpScriptGenerator.Exporters;

namespace UnrealSharpScriptGenerator;

public class CSharpExporter
{
    public void StartExport()
    {
        List<Task?> tasks = new();
        foreach (UhtPackage package in Program.Factory.Session.Packages)
        {
            ProcessPackage(package, tasks);
        }
        
        Task[] waitTasks = tasks.Where(x => x != null).Cast<Task>().ToArray();
        if (waitTasks.Length > 0)
        {
            Task.WaitAll(waitTasks);
        }
    }
    
    private void ProcessPackage(UhtType package, List<Task?> tasks)
    {
        foreach (UhtType foundHeader in package.Children)
        {
            if (foundHeader is not UhtHeaderFile header)
            {
                continue;
            }
            
            foreach (UhtType type in header.Children)
            {
                if (type is UhtClass classObj)
                {
                    if (classObj.ClassType == UhtClassType.Interface)
                    {
                        tasks.Add(Program.Factory.CreateTask(_ => { InterfaceExporter.ExportInterface(classObj); }));
                    }
                    else if (ScriptGeneratorUtilities.CanExportClass(classObj))
                    {
                        tasks.Add(Program.Factory.CreateTask(_ => { ClassExporter.ExportClass(classObj); }));
                    }
                }
                else if (type is UhtEnum enumObj)
                {
                    if (enumObj.UnderlyingType == UhtEnumUnderlyingType.Unspecified)
                    {
                        continue;
                    }
                    tasks.Add(Program.Factory.CreateTask(_ => { EnumExporter.ExportEnum(enumObj); }));
                }
                else if (type is UhtStruct structObj)
                {
                    tasks.Add(Program.Factory.CreateTask(_ => { StructExporter.ExportStruct(structObj); }));
                }
            }
        }
    }
}
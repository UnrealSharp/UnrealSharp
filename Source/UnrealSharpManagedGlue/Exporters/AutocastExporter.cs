using System.Collections.Concurrent;
using System.Collections.Generic;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.PropertyTranslators;
using UnrealSharpManagedGlue.SourceGeneration;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue.Exporters;

public static class AutocastExporter 
{
    private static readonly ConcurrentDictionary<UhtStruct, List<UhtFunction>> ExportedAutocasts = new();
    
    public static void AddAutocastFunction(UhtStruct conversionStruct, UhtFunction function)
    {
        if (!ExportedAutocasts.TryGetValue(conversionStruct, out List<UhtFunction>? value))
        {
            value = new List<UhtFunction>();
            ExportedAutocasts[conversionStruct] = value;
        }

        value.Add(function);
    }
    
    public static void StartExportingAutocastFunctions()
    {
        foreach (KeyValuePair<UhtStruct, List<UhtFunction>> pair in ExportedAutocasts)
        {
            TaskManager.StartTask(_ => 
            {
                ExportAutocast(pair.Key, pair.Value);
            });
        }
    }
    
    static void ExportAutocast(UhtStruct conversionStruct, List<UhtFunction> functions)
    {
        GeneratorStringBuilder stringBuilder = new();
        stringBuilder.StartGlueFile(conversionStruct);
        stringBuilder.DeclareType(conversionStruct, "struct", conversionStruct.GetStructName());

        string conversionStructName = conversionStruct.GetFullManagedName();
        HashSet<UhtFunction> exportedFunctions = new();
        
        foreach (UhtFunction function in functions)
        {
            string returnType = function.ReturnProperty!.GetTranslator()!.GetManagedType(function.ReturnProperty!);
            string functionCall = $"{function.Outer!.GetFullManagedName()}.{function.GetFunctionName()}";
            
            if (SharesSignature(function, exportedFunctions) || ReturnValueIsSameAsParameter(function))
            {
                continue;
            }

            stringBuilder.AppendLine($"public static implicit operator {returnType}({conversionStructName} value) => {functionCall}(value);");
            exportedFunctions.Add(function);
        }

        stringBuilder.CloseBrace();

        string directory = FileExporter.GetDirectoryPath(conversionStruct.Package);
        string fileName = $"{conversionStruct.EngineName}.Autocast";
        
        stringBuilder.EndGlueFile(conversionStruct);
        FileExporter.SaveGlueToDisk(conversionStruct.Package, directory, fileName, stringBuilder.ToString());
    }
    
    static bool SharesSignature(UhtFunction function, IEnumerable<UhtFunction> otherFunctions)
    {
        foreach (UhtFunction otherFunction in otherFunctions)
        {
            if (function == otherFunction)
            {
                continue;
            }

            bool sharesSignature = true;
            int parameterCount = function.Children.Count;
            for (int i = 0; i < parameterCount; i++)
            {
                UhtProperty parameter = (UhtProperty) function.Children[i];
                UhtProperty otherParameter = (UhtProperty) otherFunction.Children[i];

                if (parameter.IsSameType(otherParameter))
                {
                    continue;
                }
                    
                sharesSignature = false;
                break;
            }
                
            if (sharesSignature)
            {
                return true;
            }
        }
        return false;
    }
        
    static bool ReturnValueIsSameAsParameter(UhtFunction function)
    {
        UhtProperty returnProperty = function.ReturnProperty!;
        foreach (UhtType uhtType in function.Children)
        {
            UhtProperty parameter = (UhtProperty) uhtType;
            
            if (parameter != returnProperty && parameter.IsSameType(returnProperty))
            {
                return true;
            }
        }
        return false;
    }
}
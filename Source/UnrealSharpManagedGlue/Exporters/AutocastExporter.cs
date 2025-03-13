using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.PropertyTranslators;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.Exporters;

public static class AutocastExporter 
{
    private static readonly ConcurrentDictionary<UhtStruct, List<UhtFunction>?> ExportedAutocasts = new();
    
    public static void AddAutocastFunction(UhtStruct conversionStruct, UhtFunction function)
    {
        if (!ExportedAutocasts.TryGetValue(conversionStruct, out List<UhtFunction>? value))
        {
            value = new List<UhtFunction>();
            ExportedAutocasts[conversionStruct] = value;
        }

        value!.Add(function);
    }
    
    public static void StartExportingAutocastFunctions(List<Task> tasks)
    {
        foreach (KeyValuePair<UhtStruct, List<UhtFunction>?> pair in ExportedAutocasts)
        {
            tasks.Add(Program.Factory.CreateTask(_ => 
            {
                ExportAutocast(pair.Key, pair.Value);
            })!);
        }
    }
    
    static void ExportAutocast(UhtStruct conversionStruct, List<UhtFunction>? functions)
    {
        GeneratorStringBuilder stringBuilder = new();
        stringBuilder.GenerateTypeSkeleton(conversionStruct);
        stringBuilder.DeclareType(conversionStruct, "struct", conversionStruct.GetStructName());

        string conversionStructName = conversionStruct.GetFullManagedName();
        HashSet<UhtFunction> exportedFunctions = new();
        
        foreach (UhtFunction function in functions)
        {
            string returnType = PropertyTranslatorManager.GetTranslator(function.ReturnProperty!)!.GetManagedType(function.ReturnProperty!);
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
        foreach (UhtProperty parameter in function.Children)
        {
            if (parameter != returnProperty && parameter.IsSameType(returnProperty))
            {
                return true;
            }
        }
        return false;
    }
}
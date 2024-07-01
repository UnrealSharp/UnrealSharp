using System.Collections.Generic;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator;

public class InclusionList
{
    private readonly HashSet<UhtEnum> _enumerations = new();
    private readonly HashSet<UhtClass> Classes = new();
    private readonly HashSet<UhtStruct> Structs = new();
    private readonly HashSet<UhtFunction> AllFunctions = new();
    
    private readonly Dictionary<UhtStruct, HashSet<string>> _functionCategories = new();
    private readonly Dictionary<UhtStruct, HashSet<string>> _functions = new();
    private readonly Dictionary<UhtStruct, HashSet<string>> _overridableFunctions = new();
    private readonly Dictionary<UhtStruct, HashSet<UhtProperty>> _properties = new();

    public void AddEnum(UhtEnum enumType) => _enumerations.Add(enumType);
    public bool HasEnum(UhtEnum enumType) => _enumerations.Contains(enumType);

    public void AddClass(UhtClass classType) => Classes.Add(classType);
    public bool HasClass(UhtClass classType) => Classes.Contains(classType);
        
    public void AddStruct(UhtStruct structType) => Structs.Add(structType);
    public bool HasStruct(UhtStruct structType) => Structs.Contains(structType);

    public void AddAllFunctions(UhtStruct structType)
    {
        foreach (UhtFunction function in structType.Functions)
        {
            AllFunctions.Add(function);
        }
    }
    
    public void AddFunction(UhtStruct structType, string functionName) => _functions[structType].Add(functionName);
    public bool HasFunction(UhtStruct structType, string functionName) => _functions[structType].Contains(functionName);
    
    public void AddFunctionCategory(UhtStruct structType, string category) => _functionCategories[structType].Add(category);

    public void AddOverridableFunction(UhtStruct StructName, string OverridableFunctionName) => _overridableFunctions[StructName].Add(OverridableFunctionName);
    public bool HasOverridableFunction(UhtStruct StructName, string OverridableFunctionName) => _overridableFunctions[StructName].Contains(OverridableFunctionName);
        
    public void AddProperty(UhtStruct structName, UhtProperty property) => _properties[structName].Add(property);
    public bool HasProperty(UhtStruct structName, UhtProperty property) => _properties[structName].Contains(property);
}
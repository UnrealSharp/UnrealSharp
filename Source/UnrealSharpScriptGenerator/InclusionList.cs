using System.Collections.Generic;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator;

public class InclusionList
{
    private readonly HashSet<UhtEnum> _enumerations = new();
    private readonly HashSet<UhtClass> _classes = new();
    private readonly HashSet<UhtStruct> _structs = new();
    private readonly HashSet<UhtFunction> _allFunctions = new();

    private readonly Dictionary<UhtStruct, HashSet<string>> _functionCategories = new();
    private readonly Dictionary<UhtStruct, HashSet<string>> _functions = new();
    private readonly Dictionary<UhtStruct, HashSet<string>> _overridableFunctions = new();
    private readonly Dictionary<UhtStruct, HashSet<UhtProperty>> _properties = new();

    public void AddEnum(UhtEnum enumType) => _enumerations.Add(enumType);
    public bool HasEnum(UhtEnum enumType) => _enumerations.Contains(enumType);

    public void AddClass(UhtClass classType) => _classes.Add(classType);
    public bool HasClass(UhtClass classType) => _classes.Contains(classType);
        
    public void AddStruct(UhtStruct structType) => _structs.Add(structType);
    public bool HasStruct(UhtStruct structType) => _structs.Contains(structType);

    public void AddAllFunctions(UhtStruct structType)
    {
        foreach (UhtFunction function in structType.Functions)
        {
            _allFunctions.Add(function);
        }
    }
    
    public void AddFunction(UhtStruct structType, string functionName) => _functions[structType].Add(functionName);
    public bool HasFunction(UhtStruct structType, string functionName) => _functions[structType].Contains(functionName);
    
    public void AddFunctionCategory(UhtStruct structType, string category) => _functionCategories[structType].Add(category);

    public void AddOverridableFunction(UhtStruct structName, string overridableFunctionName) => _overridableFunctions[structName].Add(overridableFunctionName);
    public bool HasOverridableFunction(UhtStruct structName, string overridableFunctionName) => _overridableFunctions[structName].Contains(overridableFunctionName);
        
    public void AddProperty(UhtStruct structName, UhtProperty property) => _properties[structName].Add(property);
    public bool HasProperty(UhtStruct structName, UhtProperty property) => _properties[structName].Contains(property);
}
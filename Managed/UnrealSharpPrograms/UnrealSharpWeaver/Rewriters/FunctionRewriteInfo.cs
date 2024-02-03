using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.Rewriters;

public struct FunctionRewriteInfo(FunctionMetaData functionMetadata)
{
    public object FunctionPointerCache;
    public FieldDefinition FunctionParamSizeField;
    public Tuple<FieldDefinition, PropertyMetaData>[] FunctionParams = new Tuple<FieldDefinition, PropertyMetaData>[functionMetadata.Parameters.Length];
    public List<Tuple<FieldDefinition, PropertyMetaData>> FunctionParamsElements = new(functionMetadata.Parameters.Length);
    
    public FieldReference? FunctionPointerField => (FieldReference) FunctionPointerCache;
    public VariableDefinition? FunctionPointerVar => (VariableDefinition) FunctionPointerCache;
    
    public void SetFunctionPointerCache(object cache)
    {
        FunctionPointerCache = cache;
    }
}
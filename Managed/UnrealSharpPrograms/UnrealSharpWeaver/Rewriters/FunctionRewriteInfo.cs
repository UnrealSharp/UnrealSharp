using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.Rewriters;

public struct FunctionRewriteInfo
{
    public FunctionRewriteInfo(FunctionMetaData functionMetadata)
    {
        int paramSize = functionMetadata.Parameters.Length;

        if (functionMetadata.ReturnValue != null)
        {
            paramSize++;
        }
        
        FunctionParams = new Tuple<FieldDefinition, PropertyMetaData>[paramSize];
        FunctionParamsElements = new List<Tuple<FieldDefinition, PropertyMetaData>>(paramSize);
    }
    
    public FieldDefinition FunctionParamSizeField;
    public Tuple<FieldDefinition, PropertyMetaData>[] FunctionParams;
    public List<Tuple<FieldDefinition, PropertyMetaData>> FunctionParamsElements;
}
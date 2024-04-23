using Mono.Cecil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.TypeProcessors;

public struct FunctionRewriteInfo
{
    public FieldDefinition? FunctionParamSizeField;
    public Tuple<FieldDefinition, PropertyMetaData>[] FunctionParams;
    public List<Tuple<FieldDefinition, PropertyMetaData>> FunctionParamsElements;
    
    public FunctionRewriteInfo(FunctionMetaData functionMetadata)
    {
        var paramAmount = functionMetadata.Parameters.Length;

        if (functionMetadata.HasReturnValue())
        {
            paramAmount++;
        }
        
        FunctionParams = new Tuple<FieldDefinition, PropertyMetaData>[paramAmount];
        FunctionParamsElements = new List<Tuple<FieldDefinition, PropertyMetaData>>(paramAmount);
    }
}
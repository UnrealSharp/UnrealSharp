using Mono.Cecil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.TypeProcessors;

public struct FunctionParamRewriteInfo(PropertyMetaData propertyMetadata)
{
    public readonly PropertyMetaData PropertyMetaData = propertyMetadata;
    public FieldDefinition? OffsetField;
    public FieldDefinition? NativePropertyField;
}

public struct FunctionRewriteInfo
{
    public FunctionRewriteInfo(FunctionMetaData functionMetadata)
    {
        int paramSize = functionMetadata.Parameters.Length;

        if (functionMetadata.ReturnValue != null)
        {
            paramSize++;
        }
        
        FunctionParams = new FunctionParamRewriteInfo[paramSize];

        for (int i = 0; i < functionMetadata.Parameters.Length; i++)
        {
            FunctionParams[i] = new(functionMetadata.Parameters[i]);
        }

        if (functionMetadata.ReturnValue != null)
        {
            FunctionParams[^1] = new(functionMetadata.ReturnValue);
        }
    }
    
    public FieldDefinition? FunctionParamSizeField;
    public readonly FunctionParamRewriteInfo[] FunctionParams;
}
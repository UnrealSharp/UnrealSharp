using Mono.Cecil;

namespace UnrealSharpWeaver.MetaData;
public class VirtualFunctionMetaData : BaseMetaData
{
    public readonly FunctionMetaData FunctionMetaData;
    
    public VirtualFunctionMetaData(MethodDefinition method)
    {
        Name = method.Name;
        FunctionMetaData = new FunctionMetaData(method);
    }
}
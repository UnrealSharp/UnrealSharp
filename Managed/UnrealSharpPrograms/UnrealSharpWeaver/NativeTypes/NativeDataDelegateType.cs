using Mono.Cecil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.NativeTypes;

public class NativeDataDelegateType : NativeDataBaseDelegateType
{
    public NativeDataDelegateType(TypeReference typeRef) : base(typeRef, "SimpleDelegateMarshaller`1", PropertyType.Delegate)
    {
        
    }
}
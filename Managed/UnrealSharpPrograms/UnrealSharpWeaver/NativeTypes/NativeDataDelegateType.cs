using Mono.Cecil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.NativeTypes;

public class NativeDataDelegateType : NativeDataBaseDelegateType
{
    public NativeDataDelegateType(TypeReference typeRef, string marshallerName) : base(typeRef, marshallerName, PropertyType.Delegate)
    {
        
    }
}
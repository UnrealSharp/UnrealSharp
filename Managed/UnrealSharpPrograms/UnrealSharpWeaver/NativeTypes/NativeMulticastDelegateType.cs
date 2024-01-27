using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataMulticastDelegate : NativeDataSimpleType
{
    public FunctionMetaData Signature { get; set; }
    
    public NativeDataMulticastDelegate(TypeReference delegateType, string unrealClass, int arrayDim) 
        : base(delegateType, "DelegateMarshaller`1", unrealClass, arrayDim, PropertyType.Delegate)
    {
        TypeDefinition delegateDefinition = delegateType.Resolve();
        MethodDefinition invokeMethod = delegateDefinition.Methods.First(m => m.Name == "Broadcast");
        
        Signature = new FunctionMetaData(invokeMethod)
        {
            // Don't give a name to the delegate function, it'll cause a name collision with other delegates in the same class.
            // Let Unreal Engine handle the name generation.
            Name = "",
            FunctionFlags = FunctionFlags.Delegate | FunctionFlags.MulticastDelegate
        };
    }
}
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.Rewriters;

namespace UnrealSharpWeaver.NativeTypes;

class NativeDataMulticastDelegate : NativeDataSimpleType
{
    public FunctionMetaData Signature { get; set; }
    
    public NativeDataMulticastDelegate(TypeReference delegateType, string unrealClass, int arrayDim) 
        : base(delegateType, "DelegateMarshaller`1", unrealClass, arrayDim, PropertyType.Delegate)
    {
        foreach (TypeDefinition nestedType in delegateType.Resolve().NestedTypes)
        {
            foreach (MethodDefinition method in nestedType.Methods)
            {
                if (method.Name != "Invoke")
                {
                    continue;
                }

                Signature = new FunctionMetaData(method)
                {
                    // Don't give a name to the delegate function, it'll cause a name collision with other delegates in the same class.
                    // Let Unreal Engine handle the name generation.
                    Name = "",
                    FunctionFlags = FunctionFlags.Delegate | FunctionFlags.MulticastDelegate
                };

                return;
            }
        }
    }

    public override void WritePostInitialization(ILProcessor processor, PropertyMetaData propertyMetadata, VariableDefinition propertyPointer)
    {
        PropertyDefinition propertyRef = (PropertyDefinition) propertyMetadata.MemberRef.Resolve();
        MethodReference? Initialize = WeaverHelper.FindMethod(propertyRef.PropertyType.Resolve(), UnrealDelegateProcessor.InitializeUnrealDelegate);
        processor.Emit(OpCodes.Ldloc, propertyPointer);
        processor.Emit(OpCodes.Call, Initialize);
    }
}
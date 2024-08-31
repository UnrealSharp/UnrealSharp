using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.TypeProcessors;

namespace UnrealSharpWeaver.NativeTypes;

public class NativeDataDelegateType(TypeReference typeRef) : NativeDataBaseDelegateType(typeRef, "SingleDelegateMarshaller`1", PropertyType.Delegate)
{
    public override void WritePostInitialization(ILProcessor processor, PropertyMetaData propertyMetadata, Instruction loadNativePointer, Instruction setNativePointer)
    {
        if (!Signature.HasParameters)
        {
            return;
        }
        
        MethodReference? initialize = WeaverHelper.FindMethod(wrapperType.Resolve(), UnrealDelegateProcessor.InitializeUnrealDelegate);
        if (propertyMetadata.MemberRef is not PropertyDefinition)
        {
            VariableDefinition propertyPointer = WeaverHelper.AddVariableToMethod(processor.Body.Method, WeaverHelper.IntPtrType);
            processor.Append(loadNativePointer);
            processor.Emit(OpCodes.Ldstr, propertyMetadata.Name);
            processor.Emit(OpCodes.Call, WeaverHelper.GetNativePropertyFromNameMethod);
            processor.Emit(OpCodes.Stloc, propertyPointer);
            processor.Emit(OpCodes.Ldloc, propertyPointer);
        }
        else
        {
            processor.Append(loadNativePointer);
        }
        
        processor.Emit(OpCodes.Call, initialize);
    }
}
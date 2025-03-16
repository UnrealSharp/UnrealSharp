using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.TypeProcessors;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.NativeTypes;

public class NativeDataDelegateType(TypeReference typeRef) : NativeDataBaseDelegateType(typeRef, "SingleDelegateMarshaller`1", PropertyType.Delegate)
{
    public override void WritePostInitialization(ILProcessor processor, PropertyMetaData propertyMetadata, Instruction loadNativePointer, Instruction setNativePointer)
    {
        if (!Signature.HasParameters)
        {
            return;
        }
        
        MethodReference? initialize = wrapperType.Resolve().FindMethod(UnrealDelegateProcessor.InitializeUnrealDelegate);
        if (propertyMetadata.MemberRef is not PropertyDefinition)
        {
            VariableDefinition propertyPointer = processor.Body.Method.AddLocalVariable(WeaverImporter.IntPtrType);
            processor.Append(loadNativePointer);
            processor.Emit(OpCodes.Ldstr, propertyMetadata.Name);
            processor.Emit(OpCodes.Call, WeaverImporter.GetNativePropertyFromNameMethod);
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
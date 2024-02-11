using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.Rewriters;

namespace UnrealSharpWeaver.NativeTypes;

public class NativeDataDelegateType : NativeDataBaseDelegateType
{
    public NativeDataDelegateType(TypeReference typeRef, string marshallerName) : base(typeRef, marshallerName, PropertyType.Delegate)
    {
        
    }
    
    public override void WritePostInitialization(ILProcessor processor, PropertyMetaData propertyMetadata, VariableDefinition propertyPointer)
    {
        if (Signature.Parameters.Length == 0)
        {
            return;
        }
        
        TypeDefinition propertyRef = (TypeDefinition) propertyMetadata.MemberRef.Resolve();
        MethodReference? Initialize = WeaverHelper.FindMethod(propertyRef, UnrealDelegateProcessor.InitializeUnrealDelegate);
        
        if (propertyMetadata.MemberRef is not PropertyDefinition)
        {
            propertyPointer = WeaverHelper.AddVariableToMethod(processor.Body.Method, WeaverHelper.IntPtrType);
            processor.Emit(OpCodes.Ldloc, propertyPointer);
            processor.Emit(OpCodes.Ldstr, propertyMetadata.Name);
            processor.Emit(OpCodes.Call, WeaverHelper.GetNativePropertyFromNameMethod);
            processor.Emit(OpCodes.Stloc, propertyPointer);
        }
        
        processor.Emit(OpCodes.Ldloc, propertyPointer);
        processor.Emit(OpCodes.Call, Initialize);
    }
    
}
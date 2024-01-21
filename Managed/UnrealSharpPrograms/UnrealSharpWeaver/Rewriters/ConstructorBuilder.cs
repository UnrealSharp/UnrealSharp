using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.Rewriters;

public static class ConstructorBuilder
{
    public static MethodDefinition MakeStaticConstructor(TypeDefinition type)
    {
        return CreateConstructor(type, MethodAttributes.Static);
    }

    public static MethodDefinition CreateConstructor(TypeDefinition type, MethodAttributes attributes, params TypeReference[] parameterTypes)
    {
        MethodDefinition staticConstructor = type.GetStaticConstructor();

        if (staticConstructor != null)
        {
            return staticConstructor;
        }
        
        staticConstructor = WeaverHelper.AddMethodToType(type, ".cctor", 
            WeaverHelper.VoidTypeRef,
            attributes | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig,
            parameterTypes);

        return staticConstructor;
    }
    
    public static void CreateTypeInitializer(TypeDefinition typeDefinition, Instruction field, Instruction[] initializeInstructions)
    {
        MethodDefinition staticConstructorMethod = MakeStaticConstructor(typeDefinition);
        ILProcessor processor = staticConstructorMethod.Body.GetILProcessor();
        
        processor.Emit(OpCodes.Ldstr, typeDefinition.Name);
        
        foreach (Instruction instruction in initializeInstructions)
        {
            processor.Append(instruction);
        }
        
        processor.Append(field);
    }
    
    public static void InitializePropertyAndFunctionsResources(TypeDefinition type, FieldReference initializerField,
        List<Tuple<FieldDefinition, PropertyMetaData>>? propertyOffsetsToInitialize,
        List<Tuple<FieldDefinition, PropertyMetaData>>? propertyPointersToInitialize,
        List<Tuple<FunctionMetaData, List<Tuple<FieldDefinition, PropertyMetaData>>>>? functionParamOffsetsToInitialize,
        List<Tuple<FunctionMetaData, List<Tuple<FieldDefinition, PropertyMetaData>>>>? functionParamElementSizesToInitialize,
        Dictionary<FunctionMetaData, FieldDefinition>? functionPointersToInitialize,
        List<Tuple<FunctionMetaData, FieldDefinition>>? functionParamSizesToInitialize)
    {
        MethodDefinition staticConstructor = MakeStaticConstructor(type);
        ILProcessor processor = staticConstructor.Body.GetILProcessor();
        Instruction loadNativeClassPtr = Instruction.Create(OpCodes.Ldsfld, initializerField);
        
        if (propertyOffsetsToInitialize != null)
        {
            foreach (var offset in propertyOffsetsToInitialize)
            {
                /*
                IL_0037:  ldsfld     native int UnrealEngine.MonoRuntime.MonoTestsObject::NativeClassPtr
                  IL_003c:  ldstr      "TestObjectArray"
                  IL_0041:  call       int32 [UnrealEngine.Runtime]UnrealEngine.Runtime.UnrealInterop::GetPropertyOffsetFromName(native int,
                                                                                                                                 string)
                  IL_0046:  stsfld     int32 UnrealEngine.MonoRuntime.MonoTestsObject::TestObjectArray_Offset
                 */
                processor.Append(loadNativeClassPtr);
                processor.Emit(OpCodes.Ldstr, offset.Item2.Name);
                processor.Emit(OpCodes.Call, WeaverHelper.GetPropertyOffsetFromNameMethod);
                processor.Emit(OpCodes.Stsfld, offset.Item1);
            } 
        }

        if (propertyPointersToInitialize != null)
        {
            foreach (var nativeProp in propertyPointersToInitialize)
            {
                /*
                  IL_004b:  ldsfld     native int UnrealEngine.MonoRuntime.MonoTestsObject::NativeClassPtr
                  IL_0050:  ldstr      "TestObjectArray"
                  IL_0055:  call       native int [UnrealEngine.Runtime]UnrealEngine.Runtime.UnrealInterop::GetNativePropertyFromName(native int,
                                                                                                                                      string)
                  IL_005a:  stsfld     native int UnrealEngine.MonoRuntime.MonoTestsObject::TestObjectArray_NativeProperty
                */
                processor.Append(loadNativeClassPtr);
                processor.Emit(OpCodes.Ldstr, nativeProp.Item2.Name);
                processor.Emit(OpCodes.Call, WeaverHelper.GetNativePropertyFromNameMethod);
                processor.Emit(OpCodes.Stsfld, nativeProp.Item1);

            }
        }
        
        if (functionPointersToInitialize != null)
        {
            foreach (var nativeFunc in functionPointersToInitialize)
            {
                processor.Append(loadNativeClassPtr);
                processor.Emit(OpCodes.Ldstr, nativeFunc.Key.Name);
                processor.Emit(OpCodes.Call, WeaverHelper.GetNativeFunctionFromClassAndNameMethod);
                processor.Emit(OpCodes.Stsfld, nativeFunc.Value);
            }
        }

        if (functionParamSizesToInitialize != null)
        {
            foreach (var paramsSize in functionParamSizesToInitialize)
            {
                /*
                  IL_041a:  ldsfld     native int UnrealEngine.MonoRuntime.MonoTestsObject::TestOutParams_NativeFunction
                  IL_041f:  call       int16 [UnrealEngine.Runtime]UnrealEngine.Runtime.UnrealObject::GetNativeFunctionParamsSize(native int)
                  IL_0424:  stsfld     int32 UnrealEngine.MonoRuntime.MonoTestsObject::TestOutParams_ParamsSize
                 */
                FunctionMetaData func = paramsSize.Item1;
                FieldDefinition paramsSizeField = paramsSize.Item2;

                if (functionPointersToInitialize.TryGetValue(func, out var nativeFunc))
                {
                    processor.Emit(OpCodes.Ldsfld, nativeFunc);
                }
                else
                {
                    processor.Append(loadNativeClassPtr);
                    processor.Emit(OpCodes.Ldstr, func.Name);
                    processor.Emit(OpCodes.Call, WeaverHelper.GetNativeFunctionFromClassAndNameMethod);
                }

                processor.Emit(OpCodes.Call, WeaverHelper.GetNativeFunctionParamsSizeMethod);
                processor.Emit(OpCodes.Stsfld, paramsSizeField);
            }
        }

        if (functionParamOffsetsToInitialize != null && functionPointersToInitialize != null)
        {
            foreach (var pair in functionParamOffsetsToInitialize)
            {
                FunctionMetaData func = pair.Item1;
                var paramOffsets = pair.Item2;

                /*
                  IL_005b:  ldsfld     native int UnrealEngine.MonoRuntime.MonoTestsObject::NativeClassPtr
                  IL_005c:  ldstr      "TestOverridableFloatReturn"
                  IL_0061:  call       native int [UnrealEngine.Runtime]UnrealEngine.Runtime.UnrealObject::GetNativeFunctionFromName(native int,
                                                                                                                                     string)
                  IL_0066:  stloc.0
                 */

                Instruction? loadNativeFunction;
                if (functionPointersToInitialize.TryGetValue(func, out var nativeFunctionField))
                {
                    loadNativeFunction = processor.Create(OpCodes.Ldsfld, nativeFunctionField);
                }
                else
                {
                    VariableDefinition nativeFunctionPointer = new VariableDefinition(WeaverHelper.IntPtrType);
                    int varNum = processor.Body.Variables.Count;
                    processor.Body.Variables.Add(nativeFunctionPointer);

                    processor.Append(loadNativeClassPtr);
                    processor.Emit(OpCodes.Ldstr, func.Name);
                    processor.Emit(OpCodes.Call, WeaverHelper.GetNativeFunctionFromClassAndNameMethod);
                    processor.Emit(OpCodes.Stloc, varNum);

                    loadNativeFunction = processor.Create(OpCodes.Ldloc, varNum);
                }

                foreach (var paramPair in paramOffsets)
                {
                    FieldDefinition offsetField = paramPair.Item1;
                    PropertyMetaData param = paramPair.Item2;

                    /*
                      IL_0067:  ldloc.0
                      IL_0068:  ldstr      "X"
                      IL_006d:  call       int32 [UnrealEngine.Runtime]UnrealEngine.Runtime.UnrealInterop::GetPropertyOffsetFromName(native int,
                                                                                                                                     string)
                      IL_0072:  stsfld     int32 UnrealEngine.MonoRuntime.MonoTestUserObjectBase::TestOverridableFloatReturn_X_Offset
                     */
                    processor.Append(loadNativeFunction);
                    processor.Emit(OpCodes.Ldstr, param.Name);
                    processor.Emit(OpCodes.Call, WeaverHelper.GetPropertyOffsetFromNameMethod);
                    processor.Emit(OpCodes.Stsfld, offsetField);
                }
            }
        }

        if (functionParamElementSizesToInitialize != null && functionPointersToInitialize != null)
        {
            foreach (var pair in functionParamElementSizesToInitialize)
            {
                FunctionMetaData func = pair.Item1;
                var paramElementSizes = pair.Item2;

                /*
                  IL_005b:  ldsfld     native int UnrealEngine.MonoRuntime.MonoTestsObject::NativeClassPtr
                  IL_005c:  ldstr      "TestOverridableFloatReturn"
                  IL_0061:  call       native int [UnrealEngine.Runtime]UnrealEngine.Runtime.UnrealObject::GetNativeFunctionFromName(native int,
                                                                                                                                     string)
                  IL_0066:  stloc.0
                 */

                Instruction loadNativeFunction;
                if (functionPointersToInitialize.TryGetValue(func, out var nativeFunctionField))
                {
                    loadNativeFunction = processor.Create(OpCodes.Ldsfld, nativeFunctionField);
                }
                else
                {
                    VariableDefinition nativeFunctionPointer = new VariableDefinition(WeaverHelper.IntPtrType);
                    int varNum = processor.Body.Variables.Count;
                    processor.Body.Variables.Add(nativeFunctionPointer);

                    processor.Append(loadNativeClassPtr);
                    processor.Emit(OpCodes.Ldstr, func.Name);
                    processor.Emit(OpCodes.Call, WeaverHelper.GetNativeFunctionFromClassAndNameMethod);
                    processor.Emit(OpCodes.Stloc, varNum);

                    loadNativeFunction = processor.Create(OpCodes.Ldloc, varNum);
                }

                foreach (var paramPair in paramElementSizes)
                {
                    FieldDefinition elementSizeField = paramPair.Item1;
                    PropertyMetaData param = paramPair.Item2;

                    /*
                      IL_0067:  ldloc.0
                      IL_0068:  ldstr      "X"
                      IL_006d:  call       int32 [UnrealEngine.Runtime]UnrealEngine.Runtime.UnrealInterop::GetPropertyOffsetFromName(native int,
                                                                                                                                     string)
                      IL_0072:  stsfld     int32 UnrealEngine.MonoRuntime.MonoTestUserObjectBase::TestOverridableFloatReturn_X_Offset
                     */
                    processor.Append(loadNativeFunction);
                    processor.Emit(OpCodes.Ldstr, param.Name);
                    processor.Emit(OpCodes.Call, WeaverHelper.GetArrayElementSizeMethod);
                    processor.Emit(OpCodes.Stsfld, elementSizeField);
                }
            }
        }
        
        processor.Emit(OpCodes.Ret);
        WeaverHelper.OptimizeMethod(staticConstructor);
    } 
    
    public static void VerifySingleResult<T>(T[] results, TypeDefinition type, string endMessage)
    {
        switch (results.Length)
        {
            case 0:
                throw new RewriteException(type, $"Could not find {endMessage}");
            case > 1:
                throw new RewriteException(type, $"Found more than one {endMessage}");
        }
    }
}
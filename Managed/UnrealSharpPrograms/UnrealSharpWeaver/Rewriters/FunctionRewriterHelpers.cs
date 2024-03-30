using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.NativeTypes;

namespace UnrealSharpWeaver.Rewriters;

public static class FunctionRewriterHelpers
{
    public static void PrepareFunctionForRewrite(FunctionMetaData func, TypeDefinition classDefinition)
    {
        FieldDefinition? paramsSizeField = null;

        if (func.Parameters.Length > 0)
        {
            for (int i = 0; i < func.Parameters.Length; i++)
            {
                PropertyMetaData param = func.Parameters[i];
                AddOffsetField(classDefinition, param, func, i, ref func.RewriteInfo.FunctionParams, ref func.RewriteInfo.FunctionParamsElements);
            }

            paramsSizeField = WeaverHelper.AddFieldToType(classDefinition, $"{func.Name}_ParamsSize", WeaverHelper.Int32TypeRef);
            func.RewriteInfo.FunctionParamSizeField = paramsSizeField;
        }

        if (func.ReturnValue != null)
        {
            int index = func.Parameters.Length > 0 ? func.Parameters.Length : 0;
            AddOffsetField(classDefinition, func.ReturnValue, func, index, ref func.RewriteInfo.FunctionParams, ref func.RewriteInfo.FunctionParamsElements);
        }
        
        if (func.IsBlueprintEvent || func.IsRpc || FunctionMetaData.IsInterfaceFunction(classDefinition, func.Name))
        {
            FieldAttributes baseMethodAttributes = FieldAttributes.Private;
            FieldAttributes rpcAttributes = baseMethodAttributes | FieldAttributes.InitOnly | FieldAttributes.Static;
            FieldAttributes nativeFuncAttributes = func.IsRpc ? rpcAttributes : baseMethodAttributes;

            string nativeFuncFieldName = $"{func.Name}_NativeFunction";
            FieldDefinition nativeFunctionField = WeaverHelper.AddFieldToType(classDefinition, nativeFuncFieldName,
                WeaverHelper.IntPtrType, nativeFuncAttributes);

            RewriteMethodAsUFunctionInvoke(classDefinition, func, nativeFunctionField, paramsSizeField, func.RewriteInfo.FunctionParams);
        }
        else if (WeaverHelper.HasAnyFlags(func.FunctionFlags, FunctionFlags.BlueprintCallable | FunctionFlags.BlueprintNativeEvent))
        {
            foreach (var virtualFunction in classDefinition.Methods)
            {
                if (virtualFunction.Name != func.Name)
                {
                    continue;
                }

                if (virtualFunction.IsVirtual && virtualFunction.GetBaseMethod() != null)
                {
                    continue;
                }

                MakeManagedMethodInvoker(classDefinition, func, virtualFunction, func.RewriteInfo.FunctionParams);
                break;
            }
        }
        else
        {
            MakeManagedMethodInvoker(classDefinition, func, func.MethodDefinition, func.RewriteInfo.FunctionParams);
        }
    }

    public static MethodReference MakeMethodDeclaringTypeGeneric(MethodReference method, params TypeReference[] args)
    {
        if (args.Length == 0)
        {
            return method;
        }

        if (method.DeclaringType.GenericParameters.Count != args.Length)
        {
            throw new ArgumentException("Invalid number of generic type arguments supplied");
        }
        
        var genericTypeRef = method.DeclaringType.MakeGenericInstanceType(args);

        var newMethodRef = new MethodReference(method.Name, method.ReturnType, genericTypeRef)
        {
            HasThis = method.HasThis,
            ExplicitThis = method.ExplicitThis,
            CallingConvention = method.CallingConvention
        };

        foreach (var parameter in method.Parameters)
        {
            newMethodRef.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
        }

        foreach (var genericParam in method.GenericParameters)
        {
            newMethodRef.GenericParameters.Add(new GenericParameter(genericParam.Name, newMethodRef));
        }

        return newMethodRef;
    }

    private static void MakeManagedMethodInvoker(TypeDefinition type, FunctionMetaData func, MethodDefinition methodToCall, Tuple<FieldDefinition, PropertyMetaData>[] paramOffsetFields)
    {
        MethodDefinition invokerFunction = WeaverHelper.AddMethodToType(type, "Invoke_" + func.Name, WeaverHelper.VoidTypeRef);
        
        // Arguments Buffer from C++
        WeaverHelper.AddParameterToMethod(invokerFunction, WeaverHelper.IntPtrType);
        
        // Return Buffer to C++
        WeaverHelper.AddParameterToMethod(invokerFunction, WeaverHelper.IntPtrType);

        ILProcessor processor = invokerFunction.Body.GetILProcessor();
        Instruction loadBuffer = processor.Create(OpCodes.Ldarg_1);
        
        VariableDefinition[] paramVariables = new VariableDefinition[func.Parameters.Length];
        
        for (int i = 0; i < func.Parameters.Length; ++i)
        {
            PropertyMetaData param = func.Parameters[i];
            TypeReference paramType = WeaverHelper.ImportType(param.PropertyDataType.CSharpType);
            paramVariables[i] = WeaverHelper.AddVariableToMethod(invokerFunction, paramType);
            
            param.PropertyDataType.PrepareForRewrite(type, func, param);

            if (param.PropertyFlags.HasFlag(PropertyFlags.OutParm))
            {
                continue;
            }

            param.PropertyDataType.WriteLoad(processor, type, loadBuffer, paramOffsetFields[i].Item1, paramVariables[i]);
        }

        OpCode callOp = OpCodes.Callvirt;
        
        if (methodToCall.IsStatic)
        {
            callOp = OpCodes.Call;
        }
        else
        {
            processor.Emit(OpCodes.Ldarg_0);
            if (methodToCall.IsVirtual)
            {
                callOp = OpCodes.Call;
            }
        }

        for (var i = 0; i < paramVariables.Length; ++i)
        {
            VariableDefinition local = paramVariables[i];
            PropertyMetaData param = func.Parameters[i];
            OpCode loadCode = PropertyMetaData.IsOutParameter(param.PropertyFlags) ? OpCodes.Ldloca : OpCodes.Ldloc;
            processor.Emit(loadCode, local);
        }

        var returnIndex = 0;

        if (func.ReturnValue != null)
        {
            TypeReference returnType = WeaverHelper.ImportType(func.ReturnValue.PropertyDataType.CSharpType);
            WeaverHelper.AddVariableToMethod(invokerFunction, returnType);
            returnIndex = invokerFunction.Body.Variables.Count - 1;
        }

        processor.Emit(callOp, WeaverHelper.ImportMethod(methodToCall));

        // Marshal out params back to the native parameter buffer.
        for (int i = 0; i < paramVariables.Length; ++i)
        {
            PropertyMetaData param = func.Parameters[i];
            
            if (!PropertyMetaData.IsOutParameter(param.PropertyFlags))
            {
                continue;
            }
            
            VariableDefinition localVariable = paramVariables[i];
            FieldDefinition offsetField = paramOffsetFields[i].Item1;
            NativeDataType nativeDataParamType = param.PropertyDataType;

            Instruction loadLocalVariable = processor.Create(OpCodes.Ldloc, localVariable);
            nativeDataParamType.PrepareForRewrite(type, func, param);
            
            Instruction[] loadBufferPtr = NativeDataType.GetArgumentBufferInstructions(processor, loadBuffer, offsetField);
            
            nativeDataParamType.WriteMarshalToNative(processor, 
                type, 
                loadBufferPtr, 
                processor.Create(OpCodes.Ldc_I4_0), 
                processor.Create(OpCodes.Ldnull), 
                loadLocalVariable);
        }

        if (func.ReturnValue != null)
        {
            NativeDataType nativeReturnType = func.ReturnValue.PropertyDataType;
            processor.Emit(OpCodes.Stloc, returnIndex);

            Instruction loadReturnProperty = processor.Create(OpCodes.Ldloc, returnIndex);

            nativeReturnType.PrepareForRewrite(type, func, func.ReturnValue);
            
            nativeReturnType.WriteMarshalToNative(processor, type, [processor.Create(OpCodes.Ldarg_2)],
                processor.Create(OpCodes.Ldc_I4_0), processor.Create(OpCodes.Ldnull), loadReturnProperty);
        }

        processor.Emit(OpCodes.Ret);
        WeaverHelper.OptimizeMethod(invokerFunction);
    }

    private static FieldDefinition AddElementSizeField(TypeDefinition type, FunctionMetaData func, PropertyMetaData prop, TypeReference int32TypeRef)
    {
        return WeaverHelper.AddFieldToType(type, func.Name + "_" + prop.Name + "_ElementSize", int32TypeRef);
    }
    
    public static void RewriteMethodAsUFunctionInvoke(TypeDefinition type, 
        FunctionMetaData func, FieldDefinition nativeFunctionField, FieldDefinition? paramsSizeField,
        Tuple<FieldDefinition, PropertyMetaData>[] paramOffsetFields)
    {
        MethodDefinition? originalMethodDef = null;
        foreach (var method in type.Methods)
        {
            if (method.Name != func.Name)
            {
                continue;

            }

            originalMethodDef = method;
            break;
        }
        
        if (originalMethodDef == null)
        {
            throw new Exception($"Could not find method {func.Name} in class {type.Name}");
        }
        
        if (originalMethodDef.Body.CodeSize > 0)
        {
            string implementationMethodName = originalMethodDef.Name + "_Implementation";
            MethodDefinition implementationMethod = WeaverHelper.AddMethodToType(type, implementationMethodName, originalMethodDef.ReturnType, originalMethodDef.Attributes);
            implementationMethod.Body = originalMethodDef.Body;
            
            foreach (ParameterDefinition param in originalMethodDef.Parameters)
            {
                implementationMethod.Parameters.Add(new ParameterDefinition(param.Name, param.Attributes, WeaverHelper.ImportType(param.ParameterType)));
            }
            
            MakeManagedMethodInvoker(type, func, implementationMethod, paramOffsetFields);
        }
        
        RewriteOriginalFunctionToInvokeNative(type, func, originalMethodDef, nativeFunctionField, paramsSizeField, paramOffsetFields);
    }

    public static void RewriteOriginalFunctionToInvokeNative(TypeDefinition type, FunctionMetaData metadata,
        MethodDefinition methodDef, FieldDefinition nativeFunctionField, FieldDefinition? paramsSizeField,
        Tuple<FieldDefinition, PropertyMetaData>[] paramOffsetFields)
    {
        // Remove the original method body. We'll replace it with a call to the native function.
        methodDef.Body = new MethodBody(methodDef);

        bool staticNativeFunction = nativeFunctionField.IsStatic;
        bool hasReturnValue = methodDef.ReturnType != WeaverHelper.VoidTypeRef;
        bool hasParams = methodDef.Parameters.Count > 0 || hasReturnValue;

        ILProcessor processor = methodDef.Body.GetILProcessor();
        VariableDefinition? argumentsBufferPtr = null;
        List<Instruction> allCleanupInstructions = [];
        
        Instruction loadObjectInstance = Instruction.Create(OpCodes.Ldarg_0);
        Instruction? loadArgumentBuffer = null;
        
        if (hasParams)
        {
            WriteParametersToNative(processor, methodDef, metadata, paramsSizeField, paramOffsetFields, out argumentsBufferPtr, out loadArgumentBuffer, allCleanupInstructions);
        }
        
        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Call, WeaverHelper.NativeObjectGetter);

        if (staticNativeFunction)
        {
            processor.Emit(OpCodes.Ldsfld, nativeFunctionField);
        }
        else
        {
            processor.Append(loadObjectInstance);
            processor.Emit(OpCodes.Ldfld, nativeFunctionField);
        }

        if (hasParams)
        {
            processor.Emit(OpCodes.Ldloc, argumentsBufferPtr);
        }
        else
        {
            processor.Emit(OpCodes.Ldsfld, WeaverHelper.IntPtrZero);
        }

        processor.Emit(OpCodes.Call, WeaverHelper.InvokeNativeFunctionMethod);

        foreach (Instruction instruction in allCleanupInstructions)
        {
            processor.Append(instruction);
        }
        
        // Marshal out params back from the native parameter buffer.
        if (metadata.FunctionFlags.HasFlag(FunctionFlags.HasOutParms))
        {
            for (var i = 0; i < metadata.Parameters.Length; ++i)
            {
                PropertyMetaData param = metadata.Parameters[i];

                if (!param.PropertyFlags.HasFlag(PropertyFlags.OutParm))
                {
                    continue;
                }

                processor.Emit(OpCodes.Ldarg, i + 1);

                Instruction[] load = NativeDataType.GetArgumentBufferInstructions(processor, loadArgumentBuffer, paramOffsetFields[i].Item1);
                param.PropertyDataType.WriteMarshalFromNative(processor, type, load, processor.Create(OpCodes.Ldc_I4_0), loadObjectInstance);
            
                Instruction setInstructionOutParam = WeaverHelper.CreateSetInstructionOutParam(methodDef.Parameters[i], param.PropertyDataType.PropertyType);
                processor.Append(setInstructionOutParam);
            }
        }

        // Marshal return value back from the native parameter buffer.
        if (metadata.ReturnValue != null)
        {
            // Return value is always the last parameter.
            Instruction[] load = NativeDataType.GetArgumentBufferInstructions(processor, loadArgumentBuffer, paramOffsetFields[^1].Item1);
            metadata.ReturnValue.PropertyDataType.WriteMarshalFromNative(processor, type, load, Instruction.Create(OpCodes.Ldc_I4_0), loadObjectInstance);
        }

        processor.Emit(OpCodes.Ret);
        
        if (staticNativeFunction)
        {
            return;
        }
        
        Instruction branchTarget = processor.Body.Instructions[0];
        processor.InsertBefore(branchTarget, processor.Create(OpCodes.Ldarg_0));
        processor.InsertBefore(branchTarget, processor.Create(OpCodes.Ldfld, nativeFunctionField));
        processor.InsertBefore(branchTarget, processor.Create(OpCodes.Ldsfld, WeaverHelper.IntPtrZero));
        
        processor.InsertBefore(branchTarget, processor.Create(OpCodes.Call, WeaverHelper.IntPtrEqualsOperator));

        Instruction branchPosition = processor.Create(OpCodes.Ldarg_0);

        processor.InsertBefore(branchTarget, branchPosition);
        processor.InsertBefore(branchTarget, processor.Create(OpCodes.Ldarg_0));
        processor.InsertBefore(branchTarget, processor.Create(OpCodes.Call, WeaverHelper.NativeObjectGetter));
        processor.InsertBefore(branchTarget, processor.Create(OpCodes.Ldstr, methodDef.Name));
        processor.InsertBefore(branchTarget, processor.Create(OpCodes.Call, WeaverHelper.GetNativeFunctionFromInstanceAndNameMethod));
        processor.InsertBefore(branchTarget, processor.Create(OpCodes.Stfld, nativeFunctionField));
        processor.InsertBefore(branchPosition, processor.Create(OpCodes.Brfalse, branchTarget));
        
        WeaverHelper.OptimizeMethod(methodDef);
    }

    public static void RewriteMethodAsAsyncUFunctionImplementation(MethodDefinition methodDefinition)
    {
        methodDefinition.CustomAttributes.Clear();
        methodDefinition.Name = $"{methodDefinition.Name}_Implementation";
    }

    public static MethodDefinition CreateFunction(TypeDefinition declaringType, string name, MethodAttributes attributes, TypeReference? returnType = null, TypeReference[]? parameters = null)
    {
        if (declaringType == null)
        {
            throw new ArgumentNullException(nameof(declaringType), "Declaring type cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Method name cannot be null or whitespace.", nameof(name));
        }
        
        var def = new MethodDefinition(name, attributes, returnType ?? WeaverHelper.VoidTypeRef);

        if (parameters != null)
        {
            foreach (var type in parameters)
            {
                if (type == null)
                {
                    throw new ArgumentException("Parameter type cannot be null.", nameof(parameters));
                }
                
                def.Parameters.Add(new ParameterDefinition(type));
            }
        }

        declaringType.Methods.Add(def);

        return def;
    }
    
    public static void AddOffsetField(TypeDefinition classDefinition, PropertyMetaData propertyMetaData, FunctionMetaData func, int Index,
        ref Tuple<FieldDefinition, PropertyMetaData>[] paramOffsetFields, ref List<Tuple<FieldDefinition?, PropertyMetaData>> paramElementSizeFields)
    {
        FieldDefinition newField = WeaverHelper.AddFieldToType(classDefinition, func.Name + "_" + propertyMetaData.Name + "_Offset", WeaverHelper.Int32TypeRef);
        paramOffsetFields[Index] = Tuple.Create(newField, propertyMetaData);

        if (!propertyMetaData.PropertyDataType.NeedsElementSizeField)
        {
            return;
        }
                
        FieldDefinition elementSizeField = AddElementSizeField(classDefinition, func, propertyMetaData, WeaverHelper.Int32TypeRef);
        paramElementSizeFields[Index] = Tuple.Create(elementSizeField, propertyMetaData);
    }

    public static void WriteParametersToNative(ILProcessor processor, 
        MethodDefinition methodDef,
        FunctionMetaData metadata,
        FieldDefinition? paramsSizeField, 
        Tuple<FieldDefinition, PropertyMetaData>[] paramOffsetFields, 
        out VariableDefinition argumentsBufferPtr, 
        out Instruction loadArgumentBuffer, 
        List<Instruction> allCleanupInstructions)
    {
        // byte* ptr = stackalloc byte[TestFunction_ParamsSize];
        //IL_002b: ldsfld int32 UnrealSharp.MyActorClass::TestFunction_ParamsSize
        //IL_0030: conv.i4
        //IL_0031: localloc
        //IL_0033: stloc 0
        processor.Emit(OpCodes.Ldsfld, paramsSizeField);
        processor.Emit(OpCodes.Conv_I4);
        processor.Emit(OpCodes.Localloc);
        VariableDefinition argumentsBuffer = WeaverHelper.AddVariableToMethod(methodDef, new PointerType(WeaverHelper.ByteTypeRef));
        processor.Emit(OpCodes.Stloc, argumentsBuffer);

        // nint num = (nint) ptr;
        //IL_0037: ldloc 0
        //IL_003b: conv.i
        //IL_003c: stloc 1
        processor.Emit(OpCodes.Ldloc, argumentsBuffer);
        processor.Emit(OpCodes.Conv_I);
        argumentsBufferPtr = WeaverHelper.AddVariableToMethod(methodDef, WeaverHelper.IntPtrType);
        processor.Emit(OpCodes.Stloc, argumentsBufferPtr);
        
        loadArgumentBuffer = processor.Create(OpCodes.Ldloc, argumentsBufferPtr);
        Instruction loadParamBufferInstruction = Instruction.Create(OpCodes.Nop);
    
        for (byte i = 0; i < paramOffsetFields.Length; ++i)
        {
            PropertyMetaData paramType = paramOffsetFields[i].Item2;
            
            if (paramType.PropertyFlags.HasFlag(PropertyFlags.ReturnParm))
            {
                continue;
            }

            if (paramType.PropertyFlags.HasFlag(PropertyFlags.OutParm) && !paramType.PropertyFlags.HasFlag(PropertyFlags.ReferenceParm))
            {
                continue;
            }
        
            FieldDefinition offsetField = paramOffsetFields[i].Item1;
            NativeDataType nativeDataType = paramType.PropertyDataType;
        
            nativeDataType.PrepareForRewrite(methodDef.DeclaringType, metadata, paramOffsetFields[i].Item2);

            processor.Append(loadArgumentBuffer);
            IList<Instruction>? cleanupInstructions = nativeDataType.WriteStore(processor, methodDef.DeclaringType, loadParamBufferInstruction, offsetField, i + 1, methodDef.Parameters[i]);

            if (cleanupInstructions != null)
            {
                allCleanupInstructions.AddRange(cleanupInstructions);
            }
        }
    }
    
}
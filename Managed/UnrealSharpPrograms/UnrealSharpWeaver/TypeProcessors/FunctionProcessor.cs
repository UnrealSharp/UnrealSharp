using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.NativeTypes;

namespace UnrealSharpWeaver.TypeProcessors;

public static class FunctionProcessor
{
    public static void PrepareFunctionForRewrite(FunctionMetaData function, TypeDefinition classDefinition)
    {
        FieldDefinition? paramsSizeField = null;

        if (function.HasParameters)
        {
            for (int i = 0; i < function.Parameters.Length; i++)
            {
                PropertyMetaData param = function.Parameters[i];
                AddOffsetField(classDefinition, param, function, i, function.RewriteInfo.FunctionParams);
                AddNativePropertyField(classDefinition, param, function, i, function.RewriteInfo.FunctionParams);
            }

            paramsSizeField = WeaverHelper.AddFieldToType(classDefinition, $"{function.Name}_ParamsSize", WeaverHelper.Int32TypeRef);
            function.RewriteInfo.FunctionParamSizeField = paramsSizeField;
        }

        if (function.HasReturnValue)
        {
            int index = function.Parameters.Length > 0 ? function.Parameters.Length : 0;
            AddOffsetField(classDefinition, function.ReturnValue, function, index, function.RewriteInfo.FunctionParams);
            AddNativePropertyField(classDefinition, function.ReturnValue, function, index, function.RewriteInfo.FunctionParams);
        }
        
        if (function.IsBlueprintEvent || function.IsRpc || FunctionMetaData.IsInterfaceFunction(function.MethodDefinition))
        {
            function.FunctionPointerField = WeaverHelper.AddFieldToType(classDefinition, $"{function.Name}_NativeFunction", WeaverHelper.IntPtrType, FieldAttributes.Private);
            RewriteMethodAsUFunctionInvoke(classDefinition, function, paramsSizeField, function.RewriteInfo.FunctionParams);
        }
        else if (WeaverHelper.HasAnyFlags(function.FunctionFlags, FunctionFlags.BlueprintCallable))
        {
            foreach (var virtualFunction in classDefinition.Methods)
            {
                if (virtualFunction.Name != function.Name)
                {
                    continue;
                }

                if (virtualFunction.IsVirtual && virtualFunction.GetBaseMethod() != null)
                {
                    continue;
                }

                MakeManagedMethodInvoker(classDefinition, function, virtualFunction, function.RewriteInfo.FunctionParams);
                break;
            }
        }
        else
        {
            MakeManagedMethodInvoker(classDefinition, function, function.MethodDefinition, function.RewriteInfo.FunctionParams);
        }
    }
    
    public static void LoadNativeFunctionField(ILProcessor processor, FunctionMetaData functionMetaData)
    {
        if (functionMetaData.FunctionPointerField.IsStatic)
        {
            processor.Emit(OpCodes.Ldsfld, functionMetaData.FunctionPointerField);
        }
        else
        {
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldfld, functionMetaData.FunctionPointerField);
        }
    }
    
    public static MethodDefinition MakeImplementationMethod(FunctionMetaData func)
    {
        MethodDefinition copiedMethod = WeaverHelper.CopyMethod(func.MethodDefinition.Name + "_Implementation", func.MethodDefinition);
        if (copiedMethod.IsVirtual)
        {
            // Find the call to the original function and replace it with a call to the implementation.
            foreach (var instruction in copiedMethod.Body.Instructions)
            {
                if (instruction.OpCode != OpCodes.Call && instruction.OpCode != OpCodes.Callvirt)
                {
                    continue;
                }
                
                MethodReference calledMethod = (MethodReference) instruction.Operand;
                string engineName = WeaverHelper.GetEngineName(calledMethod.Resolve());
                
                if (engineName != func.Name)
                {
                    continue;
                }

                MethodReference implementationMethod = WeaverHelper.FindMethod(copiedMethod.DeclaringType.BaseType.Resolve(), copiedMethod.Name, false)!;
                instruction.Operand = WeaverHelper.ImportMethod(implementationMethod);
            }
        }
        return copiedMethod;
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

    private static void MakeManagedMethodInvoker(TypeDefinition type, FunctionMetaData func, MethodDefinition methodToCall, FunctionParamRewriteInfo[] paramRewriteInfos)
    {
        MethodDefinition invokerFunction = WeaverHelper.AddMethodToType(type, "Invoke_" + func.Name, 
            WeaverHelper.VoidTypeRef, 
            MethodAttributes.Private, 
            [WeaverHelper.IntPtrType, WeaverHelper.IntPtrType]);

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

            param.PropertyDataType.WriteLoad(processor, type, loadBuffer, paramRewriteInfos[i].OffsetField!, paramVariables[i]);
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
            OpCode loadCode = param.IsOutParameter ? OpCodes.Ldloca : OpCodes.Ldloc;
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
            
            if (!param.IsOutParameter)
            {
                continue;
            }
            
            VariableDefinition localVariable = paramVariables[i];
            FieldDefinition offsetField = paramRewriteInfos[i].OffsetField!;
            NativeDataType nativeDataParamType = param.PropertyDataType;

            Instruction loadLocalVariable = processor.Create(OpCodes.Ldloc, localVariable);
            nativeDataParamType.PrepareForRewrite(type, func, param);
            
            Instruction[] loadBufferPtr = NativeDataType.GetArgumentBufferInstructions(processor, loadBuffer, offsetField);
            
            nativeDataParamType.WriteMarshalToNative(processor, 
                type, 
                loadBufferPtr, 
                processor.Create(OpCodes.Ldc_I4_0), 
                loadLocalVariable);
        }

        if (func.HasReturnValue)
        {
            NativeDataType nativeReturnType = func.ReturnValue.PropertyDataType;
            processor.Emit(OpCodes.Stloc, returnIndex);

            Instruction loadReturnProperty = processor.Create(OpCodes.Ldloc, returnIndex);

            nativeReturnType.PrepareForRewrite(type, func, func.ReturnValue);
            
            nativeReturnType.WriteMarshalToNative(processor, type, [processor.Create(OpCodes.Ldarg_2)],
                processor.Create(OpCodes.Ldc_I4_0), loadReturnProperty);
        }
        
        WeaverHelper.FinalizeMethod(invokerFunction);
    }

    public static void RewriteMethodAsUFunctionInvoke(TypeDefinition type, FunctionMetaData func, FieldDefinition? paramsSizeField, FunctionParamRewriteInfo[] paramRewriteInfos)
    {
        if (func.MethodDefinition.Body != null)
        {
            MakeManagedMethodInvoker(type, func, MakeImplementationMethod(func), paramRewriteInfos);
        }
        
        RewriteOriginalFunctionToInvokeNative(type, func, func.MethodDefinition, paramsSizeField, paramRewriteInfos);
    }

    public static void RewriteOriginalFunctionToInvokeNative(TypeDefinition type, 
        FunctionMetaData metadata,
        MethodDefinition methodDef, 
        FieldDefinition? paramsSizeField,
        FunctionParamRewriteInfo[] paramRewriteInfos)
    {
        // Remove the original method body. We'll replace it with a call to the native function.
        methodDef.Body = new MethodBody(methodDef);

        bool staticNativeFunction = metadata.FunctionPointerField.IsStatic;
        bool hasReturnValue = methodDef.ReturnType != WeaverHelper.VoidTypeRef;
        bool hasParams = methodDef.Parameters.Count > 0 || hasReturnValue;

        ILProcessor processor = methodDef.Body.GetILProcessor();
        VariableDefinition? argumentsBufferPtr = null;
        List<Instruction> allCleanupInstructions = [];
        
        Instruction loadObjectInstance = Instruction.Create(OpCodes.Ldarg_0);
        Instruction? loadArgumentBuffer = null;
        
        if (hasParams)
        {
            WriteParametersToNative(processor, methodDef, metadata, paramsSizeField, paramRewriteInfos, out argumentsBufferPtr, out loadArgumentBuffer, allCleanupInstructions);
        }
        
        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Call, WeaverHelper.NativeObjectGetter);

        if (staticNativeFunction)
        {
            processor.Emit(OpCodes.Ldsfld, metadata.FunctionPointerField);
        }
        else
        {
            processor.Append(loadObjectInstance);
            processor.Emit(OpCodes.Ldfld, metadata.FunctionPointerField);
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

                Instruction[] load = NativeDataType.GetArgumentBufferInstructions(processor, loadArgumentBuffer, paramRewriteInfos[i].OffsetField!);
                param.PropertyDataType.WriteMarshalFromNative(processor, type, load, processor.Create(OpCodes.Ldc_I4_0));
            
                Instruction setInstructionOutParam = WeaverHelper.CreateSetInstructionOutParam(methodDef.Parameters[i], param.PropertyDataType.PropertyType);
                processor.Append(setInstructionOutParam);
            }
        }

        // Marshal return value back from the native parameter buffer.
        if (metadata.HasReturnValue)
        {
            // Return value is always the last parameter.
            Instruction[] load = NativeDataType.GetArgumentBufferInstructions(processor, loadArgumentBuffer, paramRewriteInfos[^1].OffsetField!);
            metadata.ReturnValue.PropertyDataType.WriteMarshalFromNative(processor, type, load, Instruction.Create(OpCodes.Ldc_I4_0));
        }

        processor.Emit(OpCodes.Ret);
        
        if (staticNativeFunction)
        {
            return;
        }
        
        Instruction branchTarget = processor.Body.Instructions[0];
        processor.InsertBefore(branchTarget, processor.Create(OpCodes.Ldarg_0));
        processor.InsertBefore(branchTarget, processor.Create(OpCodes.Ldfld, metadata.FunctionPointerField));
        processor.InsertBefore(branchTarget, processor.Create(OpCodes.Ldsfld, WeaverHelper.IntPtrZero));
        
        processor.InsertBefore(branchTarget, processor.Create(OpCodes.Call, WeaverHelper.IntPtrEqualsOperator));

        Instruction branchPosition = processor.Create(OpCodes.Ldarg_0);

        processor.InsertBefore(branchTarget, branchPosition);
        processor.InsertBefore(branchTarget, processor.Create(OpCodes.Ldarg_0));
        processor.InsertBefore(branchTarget, processor.Create(OpCodes.Call, WeaverHelper.NativeObjectGetter));
        processor.InsertBefore(branchTarget, processor.Create(OpCodes.Ldstr, methodDef.Name));
        processor.InsertBefore(branchTarget, processor.Create(OpCodes.Call, WeaverHelper.GetNativeFunctionFromInstanceAndNameMethod));
        processor.InsertBefore(branchTarget, processor.Create(OpCodes.Stfld, metadata.FunctionPointerField));
        processor.InsertBefore(branchPosition, processor.Create(OpCodes.Brfalse, branchTarget));
        
        WeaverHelper.OptimizeMethod(methodDef);
    }

    public static void RewriteMethodAsAsyncUFunctionImplementation(MethodDefinition methodDefinition)
    {
        methodDefinition.CustomAttributes.Clear();
        methodDefinition.Name = $"{methodDefinition.Name}_Implementation";
    }

    public static MethodDefinition CreateMethod(TypeDefinition declaringType, string name, MethodAttributes attributes, TypeReference? returnType = null, TypeReference[]? parameters = null)
    {
        MethodDefinition def = new MethodDefinition(name, attributes, returnType ?? WeaverHelper.VoidTypeRef);

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
    
    public static void AddOffsetField(TypeDefinition classDefinition, PropertyMetaData propertyMetaData, FunctionMetaData func, int index, FunctionParamRewriteInfo[] paramRewriteInfos)
    {
        FieldDefinition newField = WeaverHelper.AddFieldToType(classDefinition, func.Name + "_" + propertyMetaData.Name + "_Offset", WeaverHelper.Int32TypeRef);
        paramRewriteInfos[index].OffsetField = newField;
        propertyMetaData.PropertyOffsetField = newField;
    }

    public static void AddNativePropertyField(TypeDefinition classDefinition, PropertyMetaData propertyMetaData, FunctionMetaData func, int index, FunctionParamRewriteInfo[] paramRewriteInfos)
    {
        if (!propertyMetaData.PropertyDataType.NeedsNativePropertyField)
        {
            return;
        }

        var newField = WeaverHelper.AddFieldToType(classDefinition, func.Name + "_" + propertyMetaData.Name + "_NativeProperty", WeaverHelper.IntPtrType,
            FieldAttributes.InitOnly | FieldAttributes.Static | FieldAttributes.Private);
        paramRewriteInfos[index].NativePropertyField = newField;
        propertyMetaData.NativePropertyField = newField;
    }

    public static void WriteParametersToNative(ILProcessor processor, 
        MethodDefinition methodDef,
        FunctionMetaData metadata,
        FieldDefinition? paramsSizeField,
        FunctionParamRewriteInfo[] paramRewriteInfos, 
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
        
        // Initialize values
        LoadNativeFunctionField(processor, metadata);
        processor.Emit(OpCodes.Ldloc, argumentsBufferPtr);
        processor.Emit(OpCodes.Call, WeaverHelper.InitializeStructMethod);
        
        loadArgumentBuffer = processor.Create(OpCodes.Ldloc, argumentsBufferPtr);

        for (byte i = 0; i < paramRewriteInfos.Length; ++i)
        {
            PropertyMetaData paramType = paramRewriteInfos[i].PropertyMetaData;
            
            if (paramType.IsReturnParameter)
            {
                continue;
            }

            if (paramType is { IsOutParameter: true, IsReferenceParameter: false })
            {
                continue;
            }
        
            FieldDefinition offsetField = paramRewriteInfos[i].OffsetField!;
            NativeDataType nativeDataType = paramType.PropertyDataType;

            IList<Instruction>? cleanupInstructions = nativeDataType.WriteStore(processor, methodDef.DeclaringType, loadArgumentBuffer, offsetField, i + 1, methodDef.Parameters[i]);

            if (cleanupInstructions != null)
            {
                allCleanupInstructions.AddRange(cleanupInstructions);
            }
        }
    }
    
}
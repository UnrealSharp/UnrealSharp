using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Collections.Generic;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.NativeTypes;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.TypeProcessors;

public static class UnrealDelegateProcessor
{
    public static string InitializeUnrealDelegate = nameof(InitializeUnrealDelegate);
    
    public static void ProcessMulticastDelegates(List<TypeDefinition> delegateExtensions)
    {
        foreach (TypeDefinition type in delegateExtensions)
        {
            MethodReference? invokerMethod = type.FindMethod("Invoker", throwIfNotFound: false);
            
            if (invokerMethod == null)
            {
                throw new Exception("Could not find Invoker method in delegate extension type");
            }
            
            if (invokerMethod.Parameters.Count == 0)
            {
                continue;
            }
            
            FunctionMetaData functionMetaData = new FunctionMetaData(invokerMethod.Resolve());
            
            WriteInvokerMethod(invokerMethod, functionMetaData);
            ProcessInitialize(type, functionMetaData);
        }
    }
    
    public static void ProcessSingleDelegates(List<TypeDefinition> delegateExtensions, AssemblyDefinition assembly)
    {
        TypeReference delegateDataStruct = WeaverImporter.UnrealSharpAssembly.FindType("DelegateData", WeaverImporter.UnrealSharpNamespace)!;
        TypeReference blittableMarshaller = WeaverImporter.UnrealSharpCoreAssembly.FindGenericType(WeaverImporter.UnrealSharpCoreMarshallers, "BlittableMarshaller`1", [delegateDataStruct])!;
        
        MethodReference? blittabletoNativeMethod = blittableMarshaller.Resolve().FindMethod("ToNative");
        MethodReference? blittablefromNativeMethod = blittableMarshaller.Resolve().FindMethod("FromNative");
        
        if (blittabletoNativeMethod == null || blittablefromNativeMethod == null)
        {
            throw new Exception("Could not find ToNative or FromNative method in BlittableMarshaller");
        }
        
        blittabletoNativeMethod = FunctionProcessor.MakeMethodDeclaringTypeGeneric(blittabletoNativeMethod, [delegateDataStruct]);
        blittablefromNativeMethod = FunctionProcessor.MakeMethodDeclaringTypeGeneric(blittablefromNativeMethod, [delegateDataStruct]);
        
        foreach (TypeDefinition type in delegateExtensions)
        {
            TypeDefinition marshaller = assembly.CreateNewClass(type.Namespace, type.Name + "Marshaller", TypeAttributes.Class | TypeAttributes.Public);
            
            // Create a delegate from the marshaller
            MethodDefinition fromNativeMethod = marshaller.AddFromNativeMethod(type);
            MethodDefinition toNativeMethod = marshaller.AddToNativeMethod(type);
            ILProcessor processor = fromNativeMethod.Body.GetILProcessor();
            
            MethodReference constructor = type.FindMethod(".ctor", true, delegateDataStruct)!;
            constructor.DeclaringType = type;

            VariableDefinition delegateDataVar = fromNativeMethod.AddLocalVariable(delegateDataStruct);
            
            // Load native buffer
            processor.Emit(OpCodes.Ldarg_0);
            
            // Load array offset of 0
            processor.Emit(OpCodes.Ldc_I4_0);
            
            processor.Emit(OpCodes.Call, blittablefromNativeMethod);
            processor.Emit(OpCodes.Stloc, delegateDataVar);
            
            processor.Emit(OpCodes.Ldloc, delegateDataVar);
            
            MethodReference? constructorDelegate = type.FindMethod(".ctor", true, [delegateDataStruct]);
            processor.Emit(OpCodes.Newobj, constructorDelegate);
            processor.Emit(OpCodes.Ret);
            
            MethodReference? invokerMethod = type.FindMethod("Invoker");
            
            if (invokerMethod == null)
            {
                throw new Exception("Could not find Invoker method in delegate type");
            }
            
            if (invokerMethod.Parameters.Count == 0)
            {
                continue;
            }
            
            FunctionMetaData functionMetaData = new FunctionMetaData(invokerMethod.Resolve());
            
            WriteInvokerMethod(invokerMethod, functionMetaData);
            ProcessInitialize(type, functionMetaData);
        }
    }

    public static void WriteInvokerMethod(MethodReference invokerMethod, FunctionMetaData functionMetaData)
    {
        MethodDefinition invokerMethodDefinition = invokerMethod.Resolve();
        ILProcessor invokerMethodProcessor = invokerMethodDefinition.Body.GetILProcessor();
        Instruction CallBase = invokerMethodProcessor.Body.Instructions[3];
        invokerMethodProcessor.Body.Instructions.Clear();

        if (functionMetaData.Parameters.Length > 0)
        {
            functionMetaData.FunctionPointerField = invokerMethodDefinition.DeclaringType.AddField("SignatureFunction", WeaverImporter.IntPtrType, FieldAttributes.Public | FieldAttributes.Static);
            
            List<Instruction> allCleanupInstructions = [];

            for (int i = 0; i < functionMetaData.Parameters.Length; ++i)
            {
                PropertyMetaData param = functionMetaData.Parameters[i];
                NativeDataType nativeDataType = param.PropertyDataType;
                
                if (param.MemberRef == null)
                {
                    throw new Exception($"Parameter {param.Name} does not have a valid member reference");
                }

                nativeDataType.PrepareForRewrite(invokerMethodDefinition.DeclaringType, param, param.MemberRef);
            }

            FunctionProcessor.WriteParametersToNative(invokerMethodProcessor,
                invokerMethodDefinition,
                functionMetaData,
                functionMetaData.RewriteInfo.FunctionParamSizeField,
                functionMetaData.RewriteInfo.FunctionParams,
                out var loadArguments,
                out _, allCleanupInstructions);

            invokerMethodProcessor.Emit(OpCodes.Ldarg_0);
            invokerMethodProcessor.Emit(OpCodes.Ldloc, loadArguments);
        }
        else
        {
            invokerMethodProcessor.Emit(OpCodes.Ldarg_0);
            invokerMethodProcessor.Emit(OpCodes.Ldsfld, WeaverImporter.IntPtrZero);
        }
        
        invokerMethodProcessor.Append(CallBase);
        invokerMethodDefinition.FinalizeMethod();
    }
    
    static void ProcessInitialize(TypeDefinition type, FunctionMetaData functionMetaData)
    {
        MethodDefinition initializeDelegate = type.AddMethod(InitializeUnrealDelegate, 
            WeaverImporter.VoidTypeRef, MethodAttributes.Public | MethodAttributes.Static,
            [WeaverImporter.IntPtrType]);

        var processor = initializeDelegate.Body.GetILProcessor();
        
        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Call, WeaverImporter.GetSignatureFunction);
        processor.Emit(OpCodes.Stsfld, functionMetaData.FunctionPointerField);
        
        Instruction loadFunctionPointer = processor.Create(OpCodes.Ldsfld, functionMetaData.FunctionPointerField);
        functionMetaData.EmitFunctionParamOffsets(processor, loadFunctionPointer);
        functionMetaData.EmitFunctionParamSize(processor, loadFunctionPointer);
        functionMetaData.EmitParamNativeProperty(processor, loadFunctionPointer);
        
        initializeDelegate.FinalizeMethod();
    }
}
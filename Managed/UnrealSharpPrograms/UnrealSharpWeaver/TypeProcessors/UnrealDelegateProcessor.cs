using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.NativeTypes;

namespace UnrealSharpWeaver.TypeProcessors;

public static class UnrealDelegateProcessor
{
    public static string InitializeUnrealDelegate = nameof(InitializeUnrealDelegate);
    
    public static void ProcessMulticastDelegates(List<TypeDefinition> delegateExtensions)
    {
        foreach (TypeDefinition type in delegateExtensions)
        {
            MethodReference? invokerMethod = WeaverHelper.FindMethod(type, "Invoker", throwIfNotFound: false);
            
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
    
    public static void ProcessSingleDelegates(List<TypeDefinition> delegateExtensions)
    {
        TypeReference? delegateDataStruct = WeaverHelper.FindTypeInAssembly(
            WeaverHelper.BindingsAssembly, "DelegateData", WeaverHelper.UnrealSharpNamespace);
        
        TypeReference blittableMarshaller = WeaverHelper.FindGenericTypeInAssembly(
                WeaverHelper.BindingsAssembly, WeaverHelper.UnrealSharpNamespace, "BlittableMarshaller`1", [delegateDataStruct]);
        
        MethodReference? blittabletoNativeMethod = WeaverHelper.FindMethod(blittableMarshaller.Resolve(), "ToNative");
        MethodReference? blittablefromNativeMethod = WeaverHelper.FindMethod(blittableMarshaller.Resolve(), "FromNative");
        
        blittabletoNativeMethod = FunctionProcessor.MakeMethodDeclaringTypeGeneric(blittabletoNativeMethod, [delegateDataStruct]);
        blittablefromNativeMethod = FunctionProcessor.MakeMethodDeclaringTypeGeneric(blittablefromNativeMethod, [delegateDataStruct]);
        
        foreach (TypeDefinition type in delegateExtensions)
        {
            TypeDefinition marshaller = WeaverHelper.CreateNewClass(WeaverHelper.UserAssembly, type.Namespace, type.Name + "Marshaller", TypeAttributes.Class | TypeAttributes.Public);
            
            // Create a delegate from the marshaller
            MethodDefinition fromNativeMethod = WeaverHelper.AddFromNativeMethod(marshaller, type);
            MethodDefinition toNativeMethod = WeaverHelper.AddToNativeMethod(marshaller, type);
            ILProcessor processor = fromNativeMethod.Body.GetILProcessor();
            
            MethodReference? constructor = WeaverHelper.FindMethod(type, ".ctor", true, delegateDataStruct);
            constructor.DeclaringType = type;

            VariableDefinition delegateDataVar = WeaverHelper.AddVariableToMethod(fromNativeMethod, delegateDataStruct);
            
            // Load native buffer
            processor.Emit(OpCodes.Ldarg_0);
            
            // Load array offset of 0
            processor.Emit(OpCodes.Ldc_I4_0);
            
            processor.Emit(OpCodes.Call, blittablefromNativeMethod);
            processor.Emit(OpCodes.Stloc, delegateDataVar);
            
            processor.Emit(OpCodes.Ldloc, delegateDataVar);
            
            MethodReference? constructorDelegate = WeaverHelper.FindMethod(type, ".ctor", true, [delegateDataStruct]);
            processor.Emit(OpCodes.Newobj, constructorDelegate);
            processor.Emit(OpCodes.Ret);
            
            MethodReference? invokerMethod = WeaverHelper.FindMethod(type, "Invoker");
            
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
            functionMetaData.FunctionPointerField = WeaverHelper.AddFieldToType(invokerMethodDefinition.DeclaringType, "SignatureFunction", WeaverHelper.IntPtrType, FieldAttributes.Public | FieldAttributes.Static);
            
            List<Instruction> allCleanupInstructions = [];

            for (int i = 0; i < functionMetaData.Parameters.Length; ++i)
            {
                PropertyMetaData param = functionMetaData.Parameters[i];
                NativeDataType nativeDataType = param.PropertyDataType;

                nativeDataType.PrepareForRewrite(invokerMethodDefinition.DeclaringType, functionMetaData, param);
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
            invokerMethodProcessor.Emit(OpCodes.Ldsfld, WeaverHelper.IntPtrZero);
        }
        
        invokerMethodProcessor.Append(CallBase);
        invokerMethodProcessor.Emit(OpCodes.Ret);
        
        WeaverHelper.OptimizeMethod(invokerMethodDefinition);
    }
    
    static void ProcessInitialize(TypeDefinition type, FunctionMetaData functionMetaData)
    {
        MethodDefinition initializeDelegate = WeaverHelper.AddMethodToType(type, 
            InitializeUnrealDelegate, 
            WeaverHelper.VoidTypeRef, MethodAttributes.Public | MethodAttributes.Static,
            [WeaverHelper.IntPtrType]);

        var processor = initializeDelegate.Body.GetILProcessor();
        
        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Call, WeaverHelper.GetSignatureFunction);
        processor.Emit(OpCodes.Stsfld, functionMetaData.FunctionPointerField);
        
        Instruction loadFunctionPointer = processor.Create(OpCodes.Ldsfld, functionMetaData.FunctionPointerField);
        functionMetaData.EmitFunctionParamOffsets(processor, loadFunctionPointer);
        functionMetaData.EmitFunctionParamSize(processor, loadFunctionPointer);
        functionMetaData.EmitParamNativeProperty(processor, loadFunctionPointer);
        
        WeaverHelper.FinalizeMethod(initializeDelegate);
    }
}
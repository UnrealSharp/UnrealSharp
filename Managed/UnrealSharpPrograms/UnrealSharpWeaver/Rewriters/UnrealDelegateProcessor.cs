using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.Rewriters;

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
            WeaverHelper.BindingsAssembly, Program.UnrealSharpNamespace, "DelegateData");
        
        TypeReference blittableMarshaller = WeaverHelper.FindGenericTypeInAssembly(
                WeaverHelper.BindingsAssembly, Program.UnrealSharpNamespace, "BlittableMarshaller`1", [delegateDataStruct]);
        
        MethodReference? blittabletoNativeMethod = WeaverHelper.FindMethod(blittableMarshaller.Resolve(), "ToNative");
        MethodReference? blittablefromNativeMethod = WeaverHelper.FindMethod(blittableMarshaller.Resolve(), "FromNative");
        
        blittabletoNativeMethod = FunctionRewriterHelpers.MakeMethodDeclaringTypeGeneric(blittabletoNativeMethod, [delegateDataStruct]);
        blittablefromNativeMethod = FunctionRewriterHelpers.MakeMethodDeclaringTypeGeneric(blittablefromNativeMethod, [delegateDataStruct]);
        
        foreach (TypeDefinition type in delegateExtensions)
        {
            TypeDefinition marshaller = WeaverHelper.CreateNewClass(
                WeaverHelper.UserAssembly, type.Namespace, type.Name + "Marshaller", TypeAttributes.Class | TypeAttributes.Public);

            MethodDefinition toNativeMethod = WeaverHelper.AddToNativeMethod(marshaller, type);
            
            // Create a delegate from the marshaller
            MethodDefinition fromNativeMethod = WeaverHelper.AddFromNativeMethod(marshaller, type);
            ILProcessor processor = fromNativeMethod.Body.GetILProcessor();
            
            MethodReference? constructor = WeaverHelper.FindMethod(type, ".ctor", true, delegateDataStruct);
            constructor.DeclaringType = type;

            VariableDefinition delegateDataVar = WeaverHelper.AddVariableToMethod(fromNativeMethod, delegateDataStruct);
            
            // Load native buffer
            processor.Emit(OpCodes.Ldarg_0);
            
            // Load array offset of 0
            processor.Emit(OpCodes.Ldc_I4_0);
            
            // Load null
            processor.Emit(OpCodes.Ldnull);
            
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
        Instruction test = invokerMethodProcessor.Body.Instructions[3];
        invokerMethodProcessor.Body.Instructions.Clear();

        if (functionMetaData.Parameters.Length > 0)
        {
            List<Instruction> allCleanupInstructions = [];

            FunctionRewriterHelpers.WriteParametersToNative(invokerMethodProcessor,
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
        
        invokerMethodProcessor.Append(test);
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
        
        VariableDefinition signatureFunctionPointer = WeaverHelper.AddVariableToMethod(initializeDelegate, WeaverHelper.IntPtrType);
        
        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Call, WeaverHelper.GetSignatureFunction);
        processor.Emit(OpCodes.Stloc, signatureFunctionPointer);
        
        Instruction loadFunctionPointer = processor.Create(OpCodes.Ldloc, signatureFunctionPointer);
        functionMetaData.EmitFunctionParamOffsets(processor, loadFunctionPointer);
        functionMetaData.EmitFunctionParamSize(processor, loadFunctionPointer);
        functionMetaData.EmitParamElementSize(processor, loadFunctionPointer);
        
        processor.Emit(OpCodes.Ret);
    }
}
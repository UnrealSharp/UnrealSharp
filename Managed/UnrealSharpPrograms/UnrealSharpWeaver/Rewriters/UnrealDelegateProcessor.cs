using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.Rewriters;

public static class UnrealDelegateProcessor
{
    public static string InitializeUnrealDelegate = nameof(InitializeUnrealDelegate);
    
    public static void ProcessDelegateExtensions(List<TypeDefinition> delegateExtensions)
    {
        foreach (TypeDefinition type in delegateExtensions)
        {
            MethodReference? invokerMethod = WeaverHelper.FindMethod(type, "Invoker");
            
            if (invokerMethod == null)
            {
                throw new Exception("Could not find Invoker method in delegate extension type");
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
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
            // Find the Broadcast method
            TypeDefinition delegateSignatureType = null;
            
            foreach (TypeDefinition nestedType in type.NestedTypes)
            {
                foreach (MethodDefinition method in nestedType.Methods)
                {
                    if (method.Name != "Invoke")
                    {
                        continue;
                    }

                    delegateSignatureType = nestedType;
                    break;
                }
            }
            
            
            WriteInvokerMethod(type);
            ProcessInitialize(type, delegateSignatureType);
        }
    }

    public static void WriteInvokerMethod(TypeDefinition type)
    {
        MethodReference? invokerMethod = WeaverHelper.FindMethod(type, "Invoker");
        MethodDefinition invokerMethodDefinition = invokerMethod.Resolve();
        
        FunctionMetaData functionMetaData = new FunctionMetaData(invokerMethodDefinition);
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
    
    static void ProcessInitialize(TypeReference baseType, TypeDefinition delegateSignatureType)
    {
        // Get the method from the delegate
        MethodReference? invokeMethod = WeaverHelper.FindMethod(delegateSignatureType, "Invoke");
        
        if (invokeMethod.Parameters.Count == 0)
        {
            return;
        }
        
        MethodDefinition initializeDelegate = WeaverHelper.AddMethodToType(baseType.Resolve(), 
            InitializeUnrealDelegate, 
            WeaverHelper.VoidTypeRef, MethodAttributes.Public | MethodAttributes.Static,
            [WeaverHelper.IntPtrType]);
        
        FunctionMetaData functionMetaData = new FunctionMetaData(invokeMethod.Resolve());

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
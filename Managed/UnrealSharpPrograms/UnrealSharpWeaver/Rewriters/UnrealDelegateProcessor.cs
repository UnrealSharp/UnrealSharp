using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.Rewriters;

public static class UnrealDelegateProcessor
{
    public static void ProcessDelegateExtensions(List<TypeDefinition> delegateExtensions)
    {
        MethodReference multiCastDelegatePropertyExporter = 
            WeaverHelper.FindExporterMethod(Program.MulticastDelegatePropertyCallbacks, "CallBroadcastDelegate");

        TypeReference? eventDispatcher = WeaverHelper.FindTypeInAssembly(WeaverHelper.BindingsAssembly, "UnrealSharp", "EventDispatcher");
        TypeDefinition? eventDispatcherDefinition = eventDispatcher.Resolve();
        
        FieldReference nativeDelegateField = WeaverHelper.FindFieldInType(eventDispatcherDefinition, "NativeDelegate");
        
        foreach (TypeDefinition type in delegateExtensions)
        {
            // Find the Broadcast method
            MethodDefinition broadcastMethod = type.Methods.First(m => m.Name == "Broadcast");
            
            FunctionMetaData functionMetaData = new FunctionMetaData(broadcastMethod);
            var functionPointersToInitialize = new Dictionary<FunctionMetaData, FieldDefinition>();
            var functionParamSizesToInitialize = new List<Tuple<FunctionMetaData, FieldDefinition>>();
            var functionParamOffsetsToInitialize = new List<Tuple<FunctionMetaData, List<Tuple<FieldDefinition, PropertyMetaData>>>>();
            var functionParamElementSizesToInitialize = new List<Tuple<FunctionMetaData, List<Tuple<FieldDefinition, PropertyMetaData>>>>();
            
            // Only marshal the delegate if it has parameters
            if (broadcastMethod.Parameters.Count > 0)
            {
                List<FunctionMetaData> functionsToRewrite = new List<FunctionMetaData>();
                functionsToRewrite.Add(functionMetaData);
                
                FunctionRewriterHelpers.ProcessMethods
                (functionsToRewrite, type,
                    ref functionPointersToInitialize,
                    ref functionParamSizesToInitialize,
                    ref functionParamOffsetsToInitialize,
                    ref functionParamElementSizesToInitialize);

                var processor = broadcastMethod.Body.GetILProcessor();
                List<Instruction> allCleanupInstructions = [];
                
                broadcastMethod.Body.Instructions.Clear();
                
                FunctionRewriterHelpers.WriteParametersToNative(processor, 
                    broadcastMethod,
                    functionMetaData, 
                    functionParamSizesToInitialize[0].Item2, 
                    functionParamOffsetsToInitialize[0].Item2, 
                    out _,
                    out _, allCleanupInstructions);
                
                // Load the pointer to the allocated memory onto the stack
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Ldfld, nativeDelegateField);
                
                processor.Emit(OpCodes.Ldloc_1);
                
                processor.Emit(OpCodes.Call, multiCastDelegatePropertyExporter);
                processor.Emit(OpCodes.Ret);
            }
        }
    }
}
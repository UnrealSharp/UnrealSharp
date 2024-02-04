using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.Rewriters;

public static class UnrealDelegateProcessor
{
    public static void ProcessDelegateExtensions(List<TypeDefinition> delegateExtensions)
    {
        foreach (TypeDefinition type in delegateExtensions)
        {
            // Find the Broadcast method
            TypeDefinition delegateSignatureType = null;
            MethodDefinition delegateConstructor = null;
            MethodDefinition delegateInvokeMethod = null;
            
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
    
    static void ProcessFromNative(TypeReference baseType)
    {
        MethodReference? baseFromNative = WeaverHelper.FindMethod(baseType.Resolve(), "FromNative");
        MethodDefinition baseFromNativeDefinition = baseFromNative.Resolve();

        var ilProcessor = baseFromNativeDefinition.Body.GetILProcessor();
        baseFromNativeDefinition.Body.Instructions.Clear();
        
        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg_0));
        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg_1));
        ilProcessor.Append(ilProcessor.Create(OpCodes.Call, baseFromNative));
        ilProcessor.Append(ilProcessor.Create(OpCodes.Ret));
        
    }
}
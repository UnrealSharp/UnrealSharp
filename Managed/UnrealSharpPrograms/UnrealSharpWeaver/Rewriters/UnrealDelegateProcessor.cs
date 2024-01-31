using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.Rewriters;

public static class UnrealDelegateProcessor
{
    public static void ProcessDelegateExtensions(List<TypeDefinition> delegateExtensions)
    {
        TypeReference? delegateBase = WeaverHelper.FindTypeInAssembly(WeaverHelper.BindingsAssembly, "UnrealSharp", "DelegateBase`1");
        MethodReference? processDelegateMethod = WeaverHelper.FindMethod(delegateBase.Resolve(), "ProcessDelegate");
        
        foreach (TypeDefinition type in delegateExtensions)
        {
            // Find the Broadcast method
            TypeDefinition delegateSignatureType = null;
            MethodDefinition delegateConstructor = null;
            MethodDefinition delegateInvokeMethod = null;
            List<TypeDefinition> delegateSignatureTypes = [];
            
            foreach (TypeDefinition nestedType in type.NestedTypes)
            {
                foreach (MethodDefinition method in nestedType.Methods)
                {
                    if (method.Name != "Invoke")
                    {
                        continue;
                    }

                    delegateSignatureType = nestedType;
                    delegateInvokeMethod = method;
                    delegateSignatureTypes.Add(nestedType);
                    break;
                }

                foreach (MethodDefinition ctor in nestedType.GetConstructors())
                {
                    if (ctor.Parameters.Count == 2)
                    {
                        delegateConstructor = ctor;
                    }
                }
            }

            //OverrideFromNative(type, delegateSignatureType, delegateBase);
            //WriteInvokerMethod(type, processDelegateMethod);
        }
    }

    public static void WriteInvokerMethod(TypeDefinition type, MethodReference processDelegateMethod)
    {
        MethodReference? invokerMethod = WeaverHelper.FindMethod(type, "Invoker");
        MethodDefinition invokerMethodDefinition = invokerMethod.Resolve();
        
        FunctionMetaData functionMetaData = new FunctionMetaData(invokerMethodDefinition);
        ILProcessor invokerMethodProcessor = invokerMethodDefinition.Body.GetILProcessor();
        invokerMethodProcessor.Body.Instructions.Clear();
        
        invokerMethodProcessor.Emit(OpCodes.Ldarg_0);
        invokerMethodProcessor.Emit(OpCodes.Ldsfld, WeaverHelper.IntPtrZero);
        invokerMethodProcessor.Emit(OpCodes.Call, processDelegateMethod);
        invokerMethodProcessor.Emit(OpCodes.Ret);
        return;
        
        VariableDefinition? loadArguments;

        if (functionMetaData.Parameters.Length > 0)
        {
            var functionPointersToInitialize = new Dictionary<FunctionMetaData, FieldDefinition>();
            var functionParamSizesToInitialize = new List<Tuple<FunctionMetaData, FieldDefinition>>();
            var functionParamOffsetsToInitialize =
                new List<Tuple<FunctionMetaData, List<Tuple<FieldDefinition, PropertyMetaData>>>>();
            var functionParamElementSizesToInitialize =
                new List<Tuple<FunctionMetaData, List<Tuple<FieldDefinition, PropertyMetaData>>>>();

            List<FunctionMetaData> functionsToRewrite =
            [
                functionMetaData
            ];

            FunctionRewriterHelpers.ProcessMethods
            (functionsToRewrite, type,
                ref functionPointersToInitialize,
                ref functionParamSizesToInitialize,
                ref functionParamOffsetsToInitialize,
                ref functionParamElementSizesToInitialize);

            List<Instruction> allCleanupInstructions = [];

            FunctionRewriterHelpers.WriteParametersToNative(invokerMethodProcessor,
                invokerMethodDefinition,
                functionMetaData,
                functionParamSizesToInitialize[0].Item2,
                functionParamOffsetsToInitialize[0].Item2,
                out loadArguments,
                out _, allCleanupInstructions);

            invokerMethodProcessor.Emit(OpCodes.Ldarg_0);
            invokerMethodProcessor.Emit(OpCodes.Ldloc, loadArguments);
        }
        else
        {
            invokerMethodProcessor.Emit(OpCodes.Ldarg_0);
            invokerMethodProcessor.Emit(OpCodes.Ldsfld, WeaverHelper.IntPtrZero);
        }
        
        invokerMethodProcessor.Emit(OpCodes.Call, processDelegateMethod);
        invokerMethodProcessor.Emit(OpCodes.Ret);
        
        WeaverHelper.OptimizeMethod(invokerMethodDefinition);
    }


    static void OverrideFromNative(TypeDefinition type, TypeDefinition signature, TypeReference baseType)
    {
        var delegateBaseGenericInstance = new GenericInstanceType(baseType);
        delegateBaseGenericInstance.GenericArguments.Add(WeaverHelper.ImportType(signature));

        // Find the FromNative method in DelegateBase<T>
        MethodReference? baseFromNative = WeaverHelper.FindMethod(baseType.Resolve(), "FromNative");
        baseFromNative.DeclaringType = delegateBaseGenericInstance;

        MethodDefinition OverriddenFromNative = WeaverHelper.CopyMethod(baseFromNative.Resolve(), true);
        type.Methods.Add(OverriddenFromNative);

        var ilProcessor = OverriddenFromNative.Body.GetILProcessor();
        OverriddenFromNative.Body.Instructions.Clear();
        
        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg_0));
        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg_1));
        ilProcessor.Append(ilProcessor.Create(OpCodes.Call, baseFromNative));
        ilProcessor.Append(ilProcessor.Create(OpCodes.Ret));
    }
}
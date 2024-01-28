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

            OverrideFromNative(type, delegateSignatureType, delegateBase);
            
            MethodReference? invokeMethod = WeaverHelper.FindMethod(type, "GetInvoker");
            MethodDefinition getInvoker = WeaverHelper.CopyMethod(invokeMethod.Resolve(), true, delegateSignatureType);
            type.Methods.Add(getInvoker);
            
            WriteInvokerMethod(type, delegateInvokeMethod, delegateConstructor, getInvoker.Resolve(), processDelegateMethod);
        }
    }

    public static void WriteInvokerMethod(TypeDefinition type,
        MethodDefinition delegateSignature,
        MethodDefinition delegateConstructor,
        MethodDefinition getInvoker,
        MethodReference processDelegateMethod)
    {
        MethodDefinition invokerMethod = WeaverHelper.AddMethodToType(type, "Invoker", WeaverHelper.VoidTypeRef);
        FunctionMetaData functionMetaData = new FunctionMetaData(invokerMethod);
        ILProcessor invokerMethodProcessor = invokerMethod.Body.GetILProcessor();
        
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
                invokerMethod,
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
        
        WeaverHelper.OptimizeMethod(invokerMethod);
        
        ILProcessor processor = getInvoker.Body.GetILProcessor();
        getInvoker.Body.Instructions.Clear();
            
        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Ldftn, invokerMethod);
        processor.Emit(OpCodes.Newobj, delegateConstructor);
        processor.Emit(OpCodes.Ret);
        WeaverHelper.OptimizeMethod(getInvoker);
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
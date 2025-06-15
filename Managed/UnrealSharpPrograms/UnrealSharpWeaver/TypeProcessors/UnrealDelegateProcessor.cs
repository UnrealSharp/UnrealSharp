using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.NativeTypes;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.TypeProcessors;

public static class UnrealDelegateProcessor
{
    public static readonly string InitializeUnrealDelegate = "InitializeUnrealDelegate";
    
    public static void ProcessDelegates(List<TypeDefinition> delegates, List<TypeDefinition> multicastDelegates, AssemblyDefinition assembly, List<DelegateMetaData> delegateMetaData)
    {
        int totalDelegateCount = multicastDelegates.Count + delegates.Count;
        if (totalDelegateCount <= 0)
        {
            return;
        }
            
        delegateMetaData.Capacity = totalDelegateCount;
        
        ProcessMulticastDelegates(multicastDelegates, delegateMetaData);
        ProcessSingleDelegates(delegates, assembly, delegateMetaData);
    }
    
    private static void ProcessMulticastDelegates(List<TypeDefinition> delegateExtensions, List<DelegateMetaData> delegateMetaData)
    {
        foreach (TypeDefinition type in delegateExtensions)
        {
            MethodReference? invokerMethod = type.FindMethod("Invoker", throwIfNotFound: false);
            
            if (invokerMethod == null)
            {
                throw new Exception("Could not find Invoker method in delegate extension type");
            }
            
            FunctionMetaData functionMetaData = new FunctionMetaData(invokerMethod.Resolve());
            DelegateMetaData newDelegate = new DelegateMetaData(functionMetaData, 
                type, 
                "UMulticastDelegate", 
                EFunctionFlags.MulticastDelegate);
            
            delegateMetaData.Add(newDelegate);
            
            if (invokerMethod.Parameters.Count == 0)
            {
                continue;
            }
            
            WriteInvokerMethod(type, invokerMethod, functionMetaData);
            ProcessInitialize(type, functionMetaData);
        }
    }
    
    private static void ProcessSingleDelegates(List<TypeDefinition> delegateExtensions, AssemblyDefinition assembly, List<DelegateMetaData> delegateMetaData)
    {
        if (delegateExtensions.Count == 0)
        {
            return;
        }
        
        TypeReference delegateDataStruct = WeaverImporter.Instance.UnrealSharpAssembly.FindType("DelegateData", WeaverImporter.UnrealSharpNamespace)!;
        TypeReference blittableMarshaller = WeaverImporter.Instance.UnrealSharpCoreAssembly.FindGenericType(WeaverImporter.UnrealSharpCoreMarshallers, "BlittableMarshaller`1", [delegateDataStruct])!;
        
        MethodReference blittabletoNativeMethod = blittableMarshaller.Resolve().FindMethod("ToNative")!;
        MethodReference blittablefromNativeMethod = blittableMarshaller.Resolve().FindMethod("FromNative")!;
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
            
            FunctionMetaData functionMetaData = new FunctionMetaData(invokerMethod.Resolve());
            DelegateMetaData newDelegate = new DelegateMetaData(functionMetaData, 
                type, 
                "USingleDelegate");
            delegateMetaData.Add(newDelegate);
            
            if (invokerMethod.Parameters.Count == 0)
            {
                continue;
            }
            
            WriteInvokerMethod(type, invokerMethod, functionMetaData);
            ProcessInitialize(type, functionMetaData);
        }
    }

    public static void WriteInvokerMethod(TypeDefinition delegateType, MethodReference invokerMethod, FunctionMetaData functionMetaData)
    {
        GenericInstanceType baseGenericDelegateType = (GenericInstanceType)delegateType.BaseType;
        TypeReference processDelegateType = baseGenericDelegateType.GenericArguments[0];
        
        MethodReference processDelegateBase = delegateType.FindMethod("ProcessDelegate")!;
        MethodReference declaredType = FunctionProcessor.MakeMethodDeclaringTypeGeneric(processDelegateBase.Resolve().GetBaseMethod(),
                processDelegateType.ImportType()).ImportMethod();
        
        MethodDefinition invokerMethodDefinition = invokerMethod.Resolve();
        ILProcessor invokerMethodProcessor = invokerMethodDefinition.Body.GetILProcessor();
        invokerMethodProcessor.Body.Instructions.Clear();

        if (functionMetaData.Parameters.Length > 0)
        {
            functionMetaData.FunctionPointerField = delegateType.AddField("SignatureFunction", WeaverImporter.Instance.IntPtrType, FieldAttributes.Public | FieldAttributes.Static);
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
            invokerMethodProcessor.Emit(OpCodes.Ldsfld, WeaverImporter.Instance.IntPtrZero);
        }
        
        invokerMethodProcessor.Emit(OpCodes.Callvirt, declaredType);
        invokerMethodDefinition.FinalizeMethod();
    }

    public static MethodReference FindOrCreateInitializeDelegate(TypeDefinition delegateType)
    {
        MethodReference? initializeDelegate = delegateType.FindMethod(InitializeUnrealDelegate, false);
        
        if (initializeDelegate == null)
        {
            initializeDelegate = delegateType.AddMethod(InitializeUnrealDelegate, 
                WeaverImporter.Instance.VoidTypeRef, MethodAttributes.Public | MethodAttributes.Static, WeaverImporter.Instance.IntPtrType);
        }
        
        return initializeDelegate.ImportMethod();
    }
    
    static void ProcessInitialize(TypeDefinition type, FunctionMetaData functionMetaData)
    {
        MethodDefinition initializeMethod = FindOrCreateInitializeDelegate(type).Resolve();
        ILProcessor? processor = initializeMethod.Body.GetILProcessor();
        
        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Call, WeaverImporter.Instance.GetSignatureFunction);
        processor.Emit(OpCodes.Stsfld, functionMetaData.FunctionPointerField);
        
        Instruction loadFunctionPointer = processor.Create(OpCodes.Ldsfld, functionMetaData.FunctionPointerField);
        functionMetaData.EmitFunctionParamOffsets(processor, loadFunctionPointer);
        functionMetaData.EmitFunctionParamSize(processor, loadFunctionPointer);
        functionMetaData.EmitParamNativeProperty(processor, loadFunctionPointer);
        
        initializeMethod.FinalizeMethod();
    }
}
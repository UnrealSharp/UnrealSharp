using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.TypeProcessors;

public static class UnrealInterfaceProcessor
{ 
    public static void ProcessInterfaces(List<TypeDefinition> interfaces, ApiMetaData assemblyMetadata)
    {
        assemblyMetadata.InterfacesMetaData = new InterfaceMetaData[interfaces.Count];
        
        for (var i = 0; i < interfaces.Count; ++i)
        {
            TypeDefinition interfaceType = interfaces[i];
            assemblyMetadata.InterfacesMetaData[i] = new InterfaceMetaData(interfaceType);
            
            CreateInterfaceMarshaller(interfaceType);
        }
    }

    public static void CreateInterfaceMarshaller(TypeDefinition interfaceType)
    {
        TypeDefinition structMarshallerClass = WeaverHelper.CreateNewClass(WeaverHelper.UserAssembly, 
            interfaceType.Namespace, WeaverHelper.GetMarshallerClassName(interfaceType), 
            TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.BeforeFieldInit);
        
        FieldDefinition nativePointerField = WeaverHelper.AddFieldToType(structMarshallerClass, "NativeInterfaceClassPtr", 
            WeaverHelper.IntPtrType, FieldAttributes.Public | FieldAttributes.Static);
        
        string interfaceName = WeaverHelper.GetEngineName(interfaceType);
        const bool finalizeMethod = true;
        
        ConstructorBuilder.CreateTypeInitializer(structMarshallerClass, Instruction.Create(OpCodes.Stsfld, nativePointerField), 
            [Instruction.Create(OpCodes.Call, WeaverHelper.GetNativeClassFromNameMethod)], interfaceName, finalizeMethod);
        
        MakeToNativeMethod(interfaceType, structMarshallerClass, nativePointerField);
        MakeFromNativeMethod(interfaceType, structMarshallerClass, nativePointerField);
    }
    
    public static void MakeToNativeMethod(TypeDefinition interfaceType, TypeDefinition structMarshallerClass, FieldDefinition nativePointerField)
    {
        MethodDefinition toNativeMarshallerMethod = WeaverHelper.AddMethodToType(structMarshallerClass, "ToNative", 
            WeaverHelper.VoidTypeRef,
            MethodAttributes.Public | MethodAttributes.Static, WeaverHelper.IntPtrType, WeaverHelper.Int32TypeRef, interfaceType);
        
        MethodReference toNativeMethod = WeaverHelper.FindMethod(WeaverHelper.ScriptInterfaceMarshaller, "ToNative")!;
        toNativeMethod = FunctionProcessor.MakeMethodDeclaringTypeGeneric(toNativeMethod, interfaceType);
        
        ILProcessor toNativeMarshallerProcessor = toNativeMarshallerMethod.Body.GetILProcessor();
        toNativeMarshallerProcessor.Emit(OpCodes.Ldarg_0);
        toNativeMarshallerProcessor.Emit(OpCodes.Ldarg_1);
        toNativeMarshallerProcessor.Emit(OpCodes.Ldarg_2);
        toNativeMarshallerProcessor.Emit(OpCodes.Ldsfld, nativePointerField);
        toNativeMarshallerProcessor.Emit(OpCodes.Call, toNativeMethod);
        
        toNativeMarshallerMethod.FinalizeMethod();
    }
    
    public static void MakeFromNativeMethod(TypeDefinition interfaceType, TypeDefinition structMarshallerClass, FieldDefinition nativePointerField)
    {
        MethodDefinition fromNativeMarshallerMethod = WeaverHelper.AddMethodToType(structMarshallerClass, "FromNative", 
            interfaceType,
            MethodAttributes.Public | MethodAttributes.Static,
            [WeaverHelper.IntPtrType, WeaverHelper.Int32TypeRef]);
        
        MethodReference fromNativeMethod = WeaverHelper.FindMethod(WeaverHelper.ScriptInterfaceMarshaller, "FromNative")!;
        fromNativeMethod = FunctionProcessor.MakeMethodDeclaringTypeGeneric(fromNativeMethod, interfaceType);
        
        ILProcessor fromNativeMarshallerProcessor = fromNativeMarshallerMethod.Body.GetILProcessor();
        fromNativeMarshallerProcessor.Emit(OpCodes.Ldarg_0);
        fromNativeMarshallerProcessor.Emit(OpCodes.Ldarg_1);
        fromNativeMarshallerProcessor.Emit(OpCodes.Call, fromNativeMethod);
        fromNativeMarshallerProcessor.Emit(OpCodes.Ret);
        fromNativeMarshallerMethod.OptimizeMethod();
    }
}
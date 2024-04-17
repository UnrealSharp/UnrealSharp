using System.Reflection.Metadata;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.MetaData;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;
using FieldDefinition = Mono.Cecil.FieldDefinition;
using MethodDefinition = Mono.Cecil.MethodDefinition;
using TypeDefinition = Mono.Cecil.TypeDefinition;

namespace UnrealSharpWeaver.TypeProcessors;

public static class UnrealStructProcessor
{
    public static void ProcessStructs(IEnumerable<TypeDefinition> structs, ApiMetaData assemblyMetadata, AssemblyDefinition assembly)
    {
        // We need to create struct metadata in the correct order to ensure that blittable structs have
        // their UStruct attributes updated before other referencing structs use them to create UnrealTypes.
        var structStack = new Stack<TypeDefinition>();
        var pushedStructs = new HashSet<TypeDefinition>();
        var structHandlingOrder = new List<TypeDefinition>();
        var structMetadata = new Dictionary<TypeDefinition, StructMetaData>();

        foreach (var unrealStruct in structs.Where(unrealStruct => !pushedStructs.Contains(unrealStruct)))
        {
            structStack.Push(unrealStruct);
            pushedStructs.Add(unrealStruct);

            PushReferencedStructsFromAssembly(assembly, unrealStruct, structStack, pushedStructs);

            while (structStack.Count > 0) 
            {
                var currentStruct = structStack.Pop();
                try 
                {
                    if (structMetadata.ContainsKey(currentStruct)) 
                    {
                        throw new RewriteException (currentStruct, "Attempted to create struct metadata twice");
                    }
                    
                    var currentMetadata = new StructMetaData(currentStruct);
                    structHandlingOrder.Add(currentStruct);
                    structMetadata.Add(currentStruct, currentMetadata);
                } 
                catch (WeaverProcessError error) 
                {
                    ErrorEmitter.Error (error);
                }
            }
        }
        
        assemblyMetadata.StructMetaData = structMetadata.Values.ToArray();
        
        foreach (var currentStruct in structHandlingOrder) 
        {
            ProcessStruct(currentStruct, structMetadata[currentStruct]);
        }
    }
    
    private static void ProcessStruct(TypeDefinition type, StructMetaData metadata)
    {
        MethodReference? foundConstructor = WeaverHelper.FindMethod(type, ".ctor", false, WeaverHelper.IntPtrType);
        if (foundConstructor != null)
        {
            throw new RewriteException(type, "Structs cannot have a constructor that takes an IntPtr");
        }

        foreach (var prop in metadata.Fields)
        {
            prop.PropertyDataType.PrepareForRewrite(type, null, prop);
        }

        AddMarshallingToStruct(type, type, metadata);
    }

    private static void AddMarshallingToStruct(TypeDefinition type, TypeDefinition structTypeDefinition, StructMetaData metadata)
    {
        var propertyOffsetsToInitialize = new List<Tuple<FieldDefinition, PropertyMetaData>>();
        var propertyPointersToInitialize = new List<Tuple<FieldDefinition, PropertyMetaData>>();
        PropertyRewriterHelpers.ProcessClassMembers(ref propertyOffsetsToInitialize, ref propertyPointersToInitialize, structTypeDefinition, metadata.Fields);

        MethodDefinition structConstructor = ConstructorBuilder.CreateConstructor(structTypeDefinition, MethodAttributes.Public, [WeaverHelper.IntPtrType]);
        var toNativeMethod = FunctionRewriterHelpers.CreateFunction(structTypeDefinition, "ToNative", MethodAttributes.Public, null, [WeaverHelper.IntPtrType]);
        
        ILProcessor constructorBody = structConstructor.Body.GetILProcessor();
        ILProcessor toNativeBody = toNativeMethod.Body.GetILProcessor();
        Instruction loadBufferInstruction = constructorBody.Create(OpCodes.Ldarg_1);
        
        foreach (var prop in metadata.Fields)
        {
            FieldDefinition fieldDefinition = (FieldDefinition) prop.MemberRef.Resolve();
            prop.PropertyDataType.WriteLoad(constructorBody, type, loadBufferInstruction, prop.PropertyOffsetField, fieldDefinition);
            prop.PropertyDataType.WriteStore(toNativeBody, type, loadBufferInstruction, prop.PropertyOffsetField, fieldDefinition);
        }

        constructorBody.Emit(OpCodes.Ret);
        WeaverHelper.OptimizeMethod(structConstructor);

        toNativeBody.Emit(OpCodes.Ret);
        WeaverHelper.OptimizeMethod(toNativeMethod);
        
        // Create a marshaller class for the struct.
        TypeDefinition structMarshallerClass = WeaverHelper.CreateNewClass(WeaverHelper.UserAssembly, 
            structTypeDefinition.Namespace, WeaverHelper.GetMarshallerClassName(structTypeDefinition), 
            TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.BeforeFieldInit);
        
        // Field to cache the native size of the struct.
        FieldDefinition nativeStructSizeField = WeaverHelper.AddFieldToType(structTypeDefinition, "NativeDataSize", WeaverHelper.Int32TypeRef);
            
        AddFromNativeMarshallingMethod(structMarshallerClass, structTypeDefinition, nativeStructSizeField, toNativeMethod);
        AddToNativeMarshallingMethod(structMarshallerClass, structTypeDefinition, structConstructor, nativeStructSizeField);
        
        ConstructorBuilder.CreateTypeInitializer(structTypeDefinition, Instruction.Create(OpCodes.Stsfld, nativeStructSizeField), 
            [Instruction.Create(OpCodes.Call, WeaverHelper.GetNativeStructFromNameMethod), Instruction.Create(OpCodes.Call, WeaverHelper.GetNativeStructSizeMethod)]);
        
        MethodDefinition staticConstructor = ConstructorBuilder.MakeStaticConstructor(type);
        ConstructorBuilder.InitializeFields(staticConstructor, metadata.Fields.ToList(), Instruction.Create(OpCodes.Ldsfld, nativeStructSizeField));
        WeaverHelper.FinalizeMethod(staticConstructor);
    }
    
    private static void AddFromNativeMarshallingMethod(TypeDefinition marshaller, TypeDefinition structTypeDefinition, FieldDefinition nativeDataSizeField, MethodDefinition toNativeMethod)
    {
        MethodDefinition toNativeMarshallerMethod = WeaverHelper.AddMethodToType(marshaller, "ToNative", 
            WeaverHelper.VoidTypeRef,
            MethodAttributes.Public | MethodAttributes.Static,
            [WeaverHelper.IntPtrType, WeaverHelper.Int32TypeRef, structTypeDefinition]);
        
        ILProcessor toNativeMarshallerBody = toNativeMarshallerMethod.Body.GetILProcessor();
        toNativeMarshallerBody.Emit(OpCodes.Ldarga, toNativeMarshallerMethod.Parameters[2]);

        toNativeMarshallerBody.Emit(OpCodes.Ldarg_0);

        toNativeMarshallerBody.Emit(OpCodes.Ldarg_1); //arrIndex
        toNativeMarshallerBody.Emit(OpCodes.Ldsfld, nativeDataSizeField);
        toNativeMarshallerBody.Emit(OpCodes.Mul); //*

        toNativeMarshallerBody.Emit(OpCodes.Call, WeaverHelper.IntPtrAdd); //+

        toNativeMarshallerBody.Emit(OpCodes.Call, toNativeMethod);
        toNativeMarshallerBody.Emit(OpCodes.Ret);
        
        WeaverHelper.OptimizeMethod(toNativeMarshallerMethod);
    }

    //Create a marshaller mathod to Native code with signature:
    //public static <StructureType> FromNative(IntPtr nativeBuffer, int arrayIndex, UnrealObject owner)
    private static void AddToNativeMarshallingMethod(TypeDefinition marshaller, TypeDefinition structTypeDefinition, MethodDefinition ctor, FieldDefinition nativeDataSizeField)
    {
        MethodDefinition fromNativeMarshallerMethod = WeaverHelper.AddMethodToType(marshaller, "FromNative", 
            structTypeDefinition,
            MethodAttributes.Public | MethodAttributes.Static, 
            [WeaverHelper.IntPtrType, WeaverHelper.Int32TypeRef]);

        ILProcessor fromNativeMarshallerBody = fromNativeMarshallerMethod.Body.GetILProcessor();
        fromNativeMarshallerBody.Emit(OpCodes.Ldarg_0);
        fromNativeMarshallerBody.Emit(OpCodes.Ldarg_1);
        fromNativeMarshallerBody.Emit(OpCodes.Ldsfld, nativeDataSizeField);
        fromNativeMarshallerBody.Emit(OpCodes.Mul);
        fromNativeMarshallerBody.Emit(OpCodes.Call, WeaverHelper.IntPtrAdd);
        fromNativeMarshallerBody.Emit(OpCodes.Newobj, ctor);
        fromNativeMarshallerBody.Emit(OpCodes.Ret);
        
        WeaverHelper.OptimizeMethod(fromNativeMarshallerMethod);
    }

    private static void PushReferencedStructsFromAssembly(AssemblyDefinition assembly, TypeDefinition unrealStruct, Stack<TypeDefinition> structStack, HashSet<TypeDefinition> pushedStructs)
    {
        var referencedStructs = new List<TypeDefinition>();
        
        foreach (var field in unrealStruct.Fields) 
        {
            TypeDefinition fieldType = field.FieldType.Resolve();
            
            // if it's not in the same assembly, it will have been processed already
            if (assembly != fieldType.Module.Assembly) 
            {
                continue;
            }

            if (!fieldType.IsValueType || !WeaverHelper.IsUStruct(fieldType) || pushedStructs.Contains(fieldType))
            {
                continue;
            }
            
            referencedStructs.Add(fieldType);
            structStack.Push(fieldType);
            pushedStructs.Add(fieldType);
        }

        foreach (var referencedStruct in referencedStructs) 
        {
            PushReferencedStructsFromAssembly(assembly, referencedStruct, structStack, pushedStructs);
        }
    }
}
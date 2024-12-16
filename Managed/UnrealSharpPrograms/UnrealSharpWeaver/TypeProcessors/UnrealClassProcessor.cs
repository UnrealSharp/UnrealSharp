using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.TypeProcessors;

public static class UnrealClassProcessor
{ 
    public static void ProcessClasses(IList<TypeDefinition> classes, ApiMetaData assemblyMetadata)
    {
        assemblyMetadata.ClassMetaData = new List<ClassMetaData>(classes.Count);

        var rewrittenClasses = new HashSet<TypeDefinition>();
        foreach (var classDef in classes)
        {
            ProcessParentClass(classDef, classes, rewrittenClasses, assemblyMetadata);
        }
        
        foreach (ClassMetaData classMetaData in assemblyMetadata.ClassMetaData)
        {
            classMetaData.PostWeaveCleanup();
        }
    }
    
    private static void ProcessParentClass(TypeDefinition type, IList<TypeDefinition> classes, HashSet<TypeDefinition> rewrittenClasses, ApiMetaData assemblyMetadata)
    {
        TypeDefinition baseType = type.BaseType.Resolve();
        
        if (!WeaverHelper.IsValidBaseForUObject(baseType))
        {
            throw new Exception($"{type.FullName} is marked with UClass but doesn't inherit from CoreUObject.Object.");
        }
        
        if (baseType != null && baseType.Module == type.Module && classes.Contains(baseType) && !rewrittenClasses.Contains(baseType))
        {
            ProcessParentClass(baseType, classes, rewrittenClasses, assemblyMetadata);
        }

        if (rewrittenClasses.Contains(type))
        {
            return;
        }
        
        ClassMetaData classMetaData = new ClassMetaData(type);
        assemblyMetadata.ClassMetaData.Add(classMetaData);
        
        ProcessClass(type, classMetaData);
        rewrittenClasses.Add(type);
    }
    
    private static void ProcessClass(TypeDefinition classTypeDefinition, ClassMetaData metadata)
    {
        // Rewrite all the properties of the class to make getters/setters that call Native code.
        if (metadata.HasProperties)
        {
            var offsetsToInitialize = new List<Tuple<FieldDefinition, PropertyMetaData>>();
            var pointersToInitialize = new List<Tuple<FieldDefinition, PropertyMetaData>>();
            PropertyProcessor.ProcessClassMembers(ref offsetsToInitialize, ref pointersToInitialize, classTypeDefinition, metadata.Properties);
        }
        
        // Add a field to cache the native UClass pointer.
        // Example: private static readonly nint NativeClassPtr = UCoreUObjectExporter.CallGetNativeClassFromName("MyActorClass");
        FieldDefinition nativeClassField = WeaverHelper.AddFieldToType(classTypeDefinition, "NativeClass", WeaverHelper.IntPtrType);
        
        ConstructorBuilder.CreateTypeInitializer(classTypeDefinition, Instruction.Create(OpCodes.Stsfld, nativeClassField), 
            [Instruction.Create(OpCodes.Call, WeaverHelper.GetNativeClassFromNameMethod)]);

        foreach (var field in classTypeDefinition.Fields)
        {
            if (WeaverHelper.IsUProperty(field))
            {
                throw new InvalidPropertyException(field, "Fields cannot be UProperty");
            }
        }
        
        MethodDefinition staticConstructor = ConstructorBuilder.MakeStaticConstructor(classTypeDefinition);
        ILProcessor processor = staticConstructor.Body.GetILProcessor();
        Instruction loadNativeClassField = Instruction.Create(OpCodes.Ldsfld, nativeClassField);
        
        if (metadata.HasProperties)
        {
            ConstructorBuilder.InitializeFields(staticConstructor, metadata.Properties, loadNativeClassField);
        }

        foreach (var function in metadata.Functions)
        {
            EmitFunctionGlueToStaticCtor(function, processor, loadNativeClassField, staticConstructor);
        }

        foreach (var virtualFunction in metadata.VirtualFunctions)
        {
            if (!FunctionMetaData.IsInterfaceFunction(virtualFunction.MethodDef))
            {
                continue;
            }
            
            EmitFunctionGlueToStaticCtor(virtualFunction, processor, loadNativeClassField, staticConstructor);
        }

        WeaverHelper.FinalizeMethod(staticConstructor);
    }

    static void EmitFunctionGlueToStaticCtor(FunctionMetaData function, ILProcessor processor, Instruction loadNativeClassField, MethodDefinition staticConstructor)
    {
        if (!function.HasParameters)
        {
            return;
        }
            
        VariableDefinition variableDefinition = WeaverHelper.AddVariableToMethod(staticConstructor, WeaverHelper.IntPtrType);
        Instruction loadNativePointer = Instruction.Create(OpCodes.Ldloc, variableDefinition);
        Instruction storeNativePointer = Instruction.Create(OpCodes.Stloc, variableDefinition);
            
        function.EmitFunctionPointers(processor, loadNativeClassField, Instruction.Create(OpCodes.Stloc, variableDefinition));
        function.EmitFunctionParamOffsets(processor, loadNativePointer);
        function.EmitFunctionParamSize(processor, loadNativePointer);
        function.EmitParamNativeProperty(processor, loadNativePointer);
            
        foreach (var param in function.Parameters)
        {
            param.PropertyDataType.WritePostInitialization(processor, param, loadNativePointer, storeNativePointer);
        }
    }
}
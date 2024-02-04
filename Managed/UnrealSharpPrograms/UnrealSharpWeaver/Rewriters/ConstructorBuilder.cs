using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.Rewriters;

public static class ConstructorBuilder
{
    public static MethodDefinition MakeStaticConstructor(TypeDefinition type)
    {
        return CreateConstructor(type, MethodAttributes.Static);
    }

    public static MethodDefinition CreateConstructor(TypeDefinition type, MethodAttributes attributes, params TypeReference[] parameterTypes)
    {
        MethodDefinition staticConstructor = type.GetStaticConstructor();

        if (staticConstructor != null)
        {
            return staticConstructor;
        }
        
        staticConstructor = WeaverHelper.AddMethodToType(type, ".cctor", 
            WeaverHelper.VoidTypeRef,
            attributes | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig,
            parameterTypes);

        return staticConstructor;
    }
    
    public static void CreateTypeInitializer(TypeDefinition typeDefinition, Instruction field, Instruction[] initializeInstructions)
    {
        MethodDefinition staticConstructorMethod = MakeStaticConstructor(typeDefinition);
        ILProcessor processor = staticConstructorMethod.Body.GetILProcessor();
        
        processor.Emit(OpCodes.Ldstr, typeDefinition.Name);
        
        foreach (Instruction instruction in initializeInstructions)
        {
            processor.Append(instruction);
        }
        
        processor.Append(field);
    }
    
    public static void VerifySingleResult<T>(T[] results, TypeDefinition type, string endMessage)
    {
        switch (results.Length)
        {
            case 0:
                throw new RewriteException(type, $"Could not find {endMessage}");
            case > 1:
                throw new RewriteException(type, $"Found more than one {endMessage}");
        }
    }
}
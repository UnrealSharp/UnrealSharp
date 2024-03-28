using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.Rewriters;

namespace UnrealSharpWeaver.NativeTypes;

public abstract class NativeDataBaseDelegateType : NativeDataSimpleType
{
    public NativeDataBaseDelegateType(TypeReference typeRef, string marshallerName, PropertyType propertyType) 
        : base(typeRef, marshallerName, 1, propertyType)
    {
        TypeDefinition delegateTypeDef = typeRef.Resolve();
        
        foreach (TypeDefinition nestedType in delegateTypeDef.NestedTypes)
        {
            foreach (MethodDefinition method in nestedType.Methods)
            {
                if (method.Name != "Invoke")
                {
                    continue;
                }

                Signature = new FunctionMetaData(method)
                {
                    // Don't give a name to the delegate function, it'll cause a name collision with other delegates in the same class.
                    // Let Unreal Engine handle the name generation.
                    Name = "",
                    FunctionFlags = FunctionFlags.Delegate | FunctionFlags.MulticastDelegate
                };

                return;
            }
        }
    }
    
    public FunctionMetaData? Signature { get; set; }
}
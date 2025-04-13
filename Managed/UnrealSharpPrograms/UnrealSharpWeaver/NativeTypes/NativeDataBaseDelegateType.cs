using Mono.Cecil;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.NativeTypes;

public abstract class NativeDataBaseDelegateType : NativeDataSimpleType
{
    public FunctionMetaData Signature { get; set; }
    
    public TypeReference fieldType;
    public TypeReference delegateType;
    public TypeReference wrapperType;

    protected override TypeReference[] GetTypeParams()
    {
        return [delegateType.ImportType()];
    }

    public NativeDataBaseDelegateType(TypeReference typeRef, string marshallerName, PropertyType propertyType) 
        : base(typeRef, marshallerName, 1, propertyType)
    {
        fieldType = typeRef;
        delegateType = GetDelegateType(typeRef);
        wrapperType = GetWrapperType(delegateType);
        
        TypeDefinition delegateTypeDefinition = delegateType.Resolve();
        foreach (MethodDefinition method in delegateTypeDefinition.Methods)
        {
            if (method.Name != "Invoke")
            {
                continue;
            }
            
            if (!method.ReturnsVoid())
            {
                throw new Exception($"{delegateType.FullName} is exposed to Unreal Engine, and must have a void return type.");
            }

            Signature = new FunctionMetaData(method)
            {
                // Don't give a name to the delegate function, it'll cause a name collision with other delegates in the same class.
                // Let Unreal Engine handle the name generation.
                Name = "",
                FunctionFlags = EFunctionFlags.Delegate | EFunctionFlags.MulticastDelegate
            };

            return;
        }
        
        if (Signature == null)
        {
            throw new Exception("Could not find Invoke method in delegate type");
        }
    }
    
    protected TypeReference GetDelegateType(TypeReference typeRef)
    {
        return ((GenericInstanceType) typeRef).GenericArguments[0];
    }
    
    protected TypeReference GetWrapperType(TypeReference delegateType)
    {
        TypeDefinition delegateTypeDefinition = delegateType.Resolve();
        return delegateTypeDefinition.Module.Assembly.FindType($"U{delegateType.Name}", delegateType.Namespace)!;
    }
    
}
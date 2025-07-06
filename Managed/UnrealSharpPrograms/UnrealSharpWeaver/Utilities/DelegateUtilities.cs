using Mono.Cecil;

namespace UnrealSharpWeaver.Utilities;

public static class DelegateUtilities
{
    public static MethodDefinition GetDelegateInvokeMethod(TypeDefinition typeDefinition)
    {
        foreach (MethodDefinition method in typeDefinition.Methods)
        {
            if (method.Name != "Invoke")
            {
                continue;
            }
            
            if (!method.ReturnsVoid())
            {
                throw new Exception($"{typeDefinition.FullName} is exposed to Unreal Engine, and must have a void return type.");
            }
            
            return method;
        }
        
        throw new Exception($"Delegate type {typeDefinition.FullName} does not have an Invoke method.");
    }
    
    public static string GetUnrealDelegateName(TypeReference typeDefinition)
    {
        string functionName = typeDefinition.FullName;
        functionName = functionName.Replace(".", "_");
        functionName += "__DelegateSignature";
        return functionName;
    }
}
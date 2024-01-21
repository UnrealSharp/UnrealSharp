using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UnrealSharpWeaver.Rewriters;

public static class UnrealDelegateProcessor
{
    public static void ProcessDelegateExtensions(List<TypeDefinition> delegateExtensions)
    {
        foreach (TypeDefinition type in delegateExtensions)
        {
            // Find the Broadcast method
            MethodDefinition broadcastMethod = type.Methods.First(m => m.Name == "Broadcast");
            
            // Only marshal the delegate if it has parameters
            if (broadcastMethod.Parameters.Count > 0)
            {
                
            }
        }
    }
}
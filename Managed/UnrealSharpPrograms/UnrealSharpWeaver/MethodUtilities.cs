using Mono.Cecil;

namespace UnrealSharpWeaver;

public static class MethodUtilities
{
    public static bool ReturnsVoid(this MethodDefinition method)
    {
        return method.ReturnType == method.Module.TypeSystem.Void;
    }
    
    public static bool ReturnsVoid(this MethodReference method)
    {
        return ReturnsVoid(method.Resolve());
    }
}
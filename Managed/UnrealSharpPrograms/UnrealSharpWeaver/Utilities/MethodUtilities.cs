using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace UnrealSharpWeaver.Utilities;

public static class MethodUtilities
{
    public static readonly string UFunctionAttribute = "UFunctionAttribute";
    
    /// <param name="name">name the method copy will have</param>
    /// <param name="method">original method</param>
    /// <param name="addMethod">Add the method copy to the declaring type. this allows to use the original sources to be matched to the copy.</param>
    /// <param name="copyMetadataToken"></param>
    /// <returns>new instance of as copy of the original</returns>
    public static MethodDefinition CopyMethod(string name, MethodDefinition method, bool addMethod = true, bool copyMetadataToken = true)
    {
        MethodDefinition newMethod = new MethodDefinition(name, method.Attributes, method.ReturnType)
        {
            HasThis = true,
            ExplicitThis = method.ExplicitThis,
            CallingConvention = method.CallingConvention,
            Body = method.Body
        };

        if (copyMetadataToken)
        {
            newMethod.MetadataToken = method.MetadataToken;
        }

        foreach (ParameterDefinition parameter in method.Parameters)
        {
            TypeReference importedType = parameter.ParameterType.ImportType();
            newMethod.Parameters.Add(new ParameterDefinition(parameter.Name, parameter.Attributes, importedType));
        }
        
        if (addMethod)
        {
            method.DeclaringType.Methods.Add(newMethod);
        }

        return newMethod;
    }
    
    public static VariableDefinition AddLocalVariable(this MethodDefinition method, TypeReference typeReference)
    {
        var variable = new VariableDefinition(typeReference);
        method.Body.Variables.Add(variable);
        return variable;
    }
    
    public static void FinalizeMethod(this MethodDefinition method)
    {
        method.Body.GetILProcessor().Emit(OpCodes.Ret);
        OptimizeMethod(method);
    }
    
    public static bool MethodIsCompilerGenerated(this ICustomAttributeProvider method)
    {
        return method.CustomAttributes.FindAttributeByType("System.Runtime.CompilerServices", "CompilerGeneratedAttribute") != null;
    }
    
    public static void OptimizeMethod(this MethodDefinition method)
    {
        if (method.Body.CodeSize == 0)
        {
            return;
        }
        
        if (method.Body.Variables.Count > 0)
        {
            method.Body.InitLocals = true;
        }
        
        method.Body.Optimize();
        method.Body.SimplifyMacros();
    }
    
    public static void RemoveReturnInstruction(this MethodDefinition method)
    {
        if (method.Body.Instructions.Count > 0 && method.Body.Instructions[^1].OpCode == OpCodes.Ret)
        {
            method.Body.Instructions.RemoveAt(method.Body.Instructions.Count - 1);
        }
    }
    
    public static CustomAttribute? GetUFunction(this MethodDefinition function)
    {
        return function.CustomAttributes.FindAttributeByType(WeaverImporter.UnrealSharpAttributesNamespace, UFunctionAttribute);
    }
    
    public static bool IsUFunction(this MethodDefinition method)
    {
        return GetUFunction(method) != null;
    }
    
    public static MethodReference ImportMethod(this MethodReference method)
    {
        return WeaverImporter.UserAssembly.MainModule.ImportReference(method);
    }
}
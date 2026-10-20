namespace UnrealSharp.Core.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class GeneratedFunctionAttribute : Attribute
{
    public GeneratedFunctionAttribute(string functionEngineName)
    {
        FunctionEngineName = functionEngineName;
    }
    
    public string FunctionEngineName;
}
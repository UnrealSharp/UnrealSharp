using System.Reflection;

namespace UnrealSharp;

public static class DoubleSingleExtensions
{
    public static float ToFloat(this double d)
    {
        return (float)d;
    }
    
    public static double ToDouble(this float d)
    {
        return d;
    }
}

public static class TypeExtensions
{
    public static string GetAssemblyName(this Type type)
    {
        Assembly typeAssembly = type.Assembly;
        return typeAssembly.GetName().Name!;
    }
}
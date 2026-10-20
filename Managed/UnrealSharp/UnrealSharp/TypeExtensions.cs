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
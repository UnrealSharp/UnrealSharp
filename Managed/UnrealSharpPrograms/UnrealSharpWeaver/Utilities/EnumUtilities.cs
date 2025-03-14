namespace UnrealSharpWeaver.Utilities;

public static class EnumUtilities
{
    public static bool HasAnyFlags(this Enum flags, Enum testFlags)
    {
        return (Convert.ToUInt64(flags) & Convert.ToUInt64(testFlags)) != 0;
    }
}
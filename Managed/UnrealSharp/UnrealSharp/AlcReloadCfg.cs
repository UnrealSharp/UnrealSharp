namespace UnrealSharp;

public static class AlcReloadCfg
{
    private static bool _configured;
    internal static bool IsAlcReloadingEnabled;

    public static void Configure(bool alcReloadEnabled)
    {
        if (_configured)
        {
            return;
        }
        
        _configured = true;
        IsAlcReloadingEnabled = alcReloadEnabled;
    }
}

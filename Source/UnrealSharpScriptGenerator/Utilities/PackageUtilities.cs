using EpicGames.Core;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.Utilities;

public static class PackageUtilities
{
    public static string GetShortName(this UhtPackage package)
    {
        #if UE_5_5_OR_LATER 
        return package.Module.ShortName;
        #else
        return package.ShortName;
        #endif
    }
    
    public static bool IsPartOfEngine(this UhtPackage package)
    {
        #if UE_5_5_OR_LATER 
        return package.Module.IsPartOfEngine;
        #else
        return package.IsPartOfEngine;
        #endif
    }
    
    public static UHTManifest.Module GetModule(this UhtPackage package)
    {
        #if UE_5_5_OR_LATER 
        return package.Module.Module;
        #else
        return package.Module;
        #endif
    }
}
using System.Collections.Generic;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.Utilities;

public static class HeaderUtilities
{

    public static IEnumerable<UhtPackage> GetPackages(this UhtHeaderFile header)
    {
        #if UE_5_5_OR_LATER
        return header.Module.Packages;
        #else
        return [header.Package];
        #endif
    }
}

using System.Collections.Generic;
using EpicGames.Core;
using EpicGames.UHT.Types;

namespace UnrealSharpManagedGlue.Utilities;

public static class PackageUtilities
{
    public const string SkipGlueGenerationDefine = "SkipGlueGeneration";

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
        bool isPartOfEngine = false;
        #if UE_5_5_OR_LATER
        isPartOfEngine = package.Module.IsPartOfEngine;
        #else
        isPartOfEngine = package.IsPartOfEngine;
        #endif

        return isPartOfEngine || package.IsForcedAsEngineGlue();
    }

    public static bool IsPlugin(this UhtPackage package)
    {
        #if UE_5_5_OR_LATER
        return package.Module.IsPlugin;
        #else
        return package.IsPlugin;
        #endif
    }
    
    public static bool IsEditorOnly(this UhtPackage package)
    {
        return package.PackageFlags.HasAnyFlags(EPackageFlags.EditorOnly | EPackageFlags.UncookedOnly | EPackageFlags.Developer);
    }

    public static bool IsForcedAsEngineGlue(this UhtPackage package)
    {
        bool hasDefine = package.GetModule().TryGetDefine("ForceAsEngineGlue", out int treatedAsEngineGlue);
        return hasDefine && treatedAsEngineGlue != 0;
    }

    public static UHTManifest.Module GetModule(this UhtPackage package)
    {
        #if UE_5_5_OR_LATER
        return package.Module.Module;
        #else
        return package.Module;
        #endif
    }

    public static bool ShouldExportPackage(this UhtPackage package)
    {
        bool foundDefine = package.GetModule().PublicDefines.Contains(SkipGlueGenerationDefine);
        return !foundDefine;
    }

    public static IReadOnlyCollection<UhtHeaderFile> GetHeaderFiles(this UhtPackage package)
    {
        #if UE_5_5_OR_LATER
        return package.Module.Headers;
        #else
        return package.Children
            .OfType<UhtHeaderFile>()
            .ToList();
        #endif
    }
}

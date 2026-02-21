using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EpicGames.Core;
using EpicGames.UHT.Types;

namespace UnrealSharpManagedGlue.Utilities;

public static class PackageUtilities
{
    public const string SkipGlueGenerationDefine = "SkipGlueGeneration";
    public const string FlattenGlueDefine = "FlattenGlue";

    public static string GetModuleShortName(this UhtPackage package)
    {
        return package.Module.ShortName;
    }

    public static bool IsPartOfEngine(this UhtPackage package)
    {
        return package.Module.IsPartOfEngine || package.IsForcedAsEngineGlue();
    }
    
    public static bool IsDefineActive(this UhtPackage package, string define)
    {
        UHTManifest.Module module = package.GetModule();
        return module.TryGetDefine(define, out int value) && value != 0;
    }
    
    public static bool IsEditorOnly(this UhtPackage package)
    {
        return package.PackageFlags.HasAnyFlags(EPackageFlags.EditorOnly | EPackageFlags.UncookedOnly | EPackageFlags.Developer);
    }

    public static bool ShouldFlattenGlue(this UhtPackage package)
    {
        return package.IsDefineActive(FlattenGlueDefine);
    }

    public static bool IsForcedAsEngineGlue(this UhtPackage package)
    {
        bool hasDefine = package.GetModule().TryGetDefine("ForceAsEngineGlue", out int treatedAsEngineGlue);
        return hasDefine && treatedAsEngineGlue != 0;
    }

    public static UHTManifest.Module GetModule(this UhtPackage package)
    {
        return package.Module.Module;
    }

    public static bool ShouldExportPackage(this UhtPackage package)
    {
        bool foundDefine = package.IsDefineActive(SkipGlueGenerationDefine);
        return !foundDefine;
    }
    
    public static IReadOnlyCollection<UhtHeaderFile> GetHeaderFiles(this UhtPackage package)
    {
        return package.Module.Headers;
    }

    public static string GetBaseDirectoryForPackage(this UhtPackage package)
    {
        DirectoryInfo? currentDirectory = new DirectoryInfo(package.GetModule().BaseDirectory);
        FileInfo? projectFile = null;

        while (currentDirectory != null)
        {
            projectFile = currentDirectory.EnumerateFiles("*.*", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(f => f.Extension.Equals(".uplugin", StringComparison.OrdinalIgnoreCase) || 
                                     f.Extension.Equals(".uproject", StringComparison.OrdinalIgnoreCase));

            if (projectFile != null)
            {
                break;
            }
                
            currentDirectory = currentDirectory.Parent;
        }

        if (projectFile == null || currentDirectory == null)
        {
            throw new InvalidOperationException($"Could not find .uplugin or .uproject for {package.SourceName}");
        }

        return currentDirectory.FullName;
    }
    
    public static string GetModuleUhtOutputDirectory(this UhtPackage package)
    {
        return Path.Combine(package.GetUhtBaseOutputDirectory(), package.GetModuleShortName());
    }
    
    public static string GetUhtBaseOutputDirectory(this UhtPackage package)
    {
        ModuleInfo moduleInfo = package.GetModuleInfo();
        string root = moduleInfo.IsPartOfEngine ? GeneratorStatics.BindingsProjectDirectory : moduleInfo.ProjectDirectory;
        string subPath = Path.Combine(root, "obj", "UHT", GeneratorStatics.BuildTarget.ToString());
        return subPath;
    }
}

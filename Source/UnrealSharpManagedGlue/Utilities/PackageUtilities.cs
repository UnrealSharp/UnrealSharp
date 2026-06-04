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

    public static bool IsForcedAsEngineGlue(this UhtPackage package)
    {
        bool hasDefine = package.GetModule().TryGetDefine("ForceAsEngineGlue", out int treatedAsEngineGlue);
        return hasDefine && treatedAsEngineGlue != 0;
    }

    public static string GetPackageExtensions(this UhtPackage package)
    {
        package.GetModule().TryGetDefine("ExtendModule", out string? extension);
        return extension ?? string.Empty;
    }
    
    public static List<string> GetAdditionalExtensionFolders(this UhtPackage package)
    {
        package.GetModule().TryGetDefine("AdditionalExtensionFolder", out string? extensionFolders);
        
        if (string.IsNullOrEmpty(extensionFolders))
        {
            return [];
        }
        
        return extensionFolders.Split(';').ToList();
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

    public static string GetBaseDirectoryForPackage(this UhtPackage package)
    {
        return LocateProjectOrPluginRoot(package.GetModule().BaseDirectory);
    }
    
    public static string LocateProjectOrPluginRoot(string directory)
    {
        DirectoryInfo? currentDirectory = new DirectoryInfo(directory);
        FileInfo? rootFile = null;

        while (currentDirectory != null)
        {
            IEnumerable<FileInfo> foundFiles = currentDirectory.EnumerateFiles("*.*", SearchOption.TopDirectoryOnly);
            
            rootFile = foundFiles.FirstOrDefault(f => IsUPluginFile(f.FullName) || IsUProjectFile(f.FullName));
            if (rootFile != null)
            {
                break;
            }
                
            currentDirectory = currentDirectory.Parent;
        }

        if (rootFile == null)
        {
            throw new InvalidOperationException($"Could not find .uproject or .uplugin file in any parent directory of {directory}");
        }

        return currentDirectory!.FullName;
    }
    
    public static string GetPackageOutputDirectory(this UhtPackage package)
    {
        ModuleInfo moduleInfo = package.GetModuleInfo();
        return moduleInfo.GlueOutputDirectory;
    }
    
    public static string GetUHTBaseDirectory(this UhtPackage package)
    {
        DirectoryInfo glueOutputDirectory = new DirectoryInfo(package.GetPackageOutputDirectory());
        return glueOutputDirectory.Parent!.FullName;
    }
    
    private static bool IsUPluginFile(string filePath)
    {
        return HasExtension(filePath, ".uplugin");
    }
    
    private static bool IsUProjectFile(string filePath)
    {
        return HasExtension(filePath, ".uproject");
    }
    
    private static bool HasExtension(string filePath, string extension)
    {
        string extensionName = Path.GetExtension(filePath);
        return string.Equals(extensionName, extension, StringComparison.OrdinalIgnoreCase);
    }
}

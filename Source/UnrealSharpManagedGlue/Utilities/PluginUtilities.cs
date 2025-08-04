using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.Utilities;

public static class PluginUtilities
{
    public static readonly Dictionary<UhtPackage, ProjectDirInfo> PluginInfo = new();

    public static ProjectDirInfo FindOrAddProjectInfo(this UhtPackage package)
    {
        if (PluginInfo.TryGetValue(package, out ProjectDirInfo plugin))
        {
            return plugin;
        }
        
        string baseDirectory = package.GetModule().BaseDirectory;
        DirectoryInfo? currentDirectory = new DirectoryInfo(baseDirectory);

        FileInfo? projectFile = null;
        while (currentDirectory is not null)
        {
            FileInfo[] foundFiles = currentDirectory.GetFiles("*.*", SearchOption.TopDirectoryOnly);
            projectFile = foundFiles.FirstOrDefault(f => f.Extension.Equals(".uplugin", StringComparison.OrdinalIgnoreCase) || 
                                                         f.Extension.Equals(".uproject", StringComparison.OrdinalIgnoreCase));
            
            if (projectFile is not null)
            {
                break;
            }

            currentDirectory = currentDirectory.Parent;
        }
        
        if (projectFile is null)
        {
            throw new InvalidOperationException($"Could not find .uplugin or .uproject file for package {package.SourceName} in {baseDirectory}");
        }
        
        HashSet<string> dependencies = new HashSet<string>();
        foreach (UhtHeaderFile header in package.GetHeaderFiles())
        {
            HashSet<UhtHeaderFile> referencedHeaders = header.References.ReferencedHeaders;
            referencedHeaders.UnionWith(header.ReferencedHeadersNoLock);
            
            foreach (UhtHeaderFile refHeader in referencedHeaders)
            {
                foreach (UhtPackage refPackage in refHeader.GetPackages())
                {
                    if (refPackage.IsPartOfEngine() || refPackage == package)
                    {
                        continue;
                    }
                    
                    ProjectDirInfo projectInfo = refPackage.FindOrAddProjectInfo();
                    dependencies.Add(projectInfo.GlueCsProjPath);
                }
            }
        }
        
        ProjectDirInfo info = new ProjectDirInfo(Path.GetFileNameWithoutExtension(projectFile.Name), currentDirectory!.FullName, dependencies);
        PluginInfo.Add(package, info);
        
        return info;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnrealSharp.UnrealSharpCore;

namespace UnrealSharp.CoreUObject;

public partial class FPackageName
{
    /// <summary>
    /// This will insert a mount point at the head of the search chain (so it can overlap an existing mount point and win).
    /// </summary>
    /// <param name="rootPath">Logical Root Path.</param>
    /// <param name="contentPath">Content Path on disk.</param>
    public static void RegisterMountPoint(string rootPath, string contentPath)
    {
        UCSPackageNameExtensions.RegisterMountPoint(rootPath, contentPath);
    }

    /// <summary>
    /// This will remove a previously inserted mount point.
    /// </summary>
    /// <param name="rootPath">Logical Root Path.</param>
    /// <param name="contentPath">Content Path on disk.</param>
    public static void UnRegisterMountPoint(string rootPath, string contentPath)
    {
        UCSPackageNameExtensions.UnRegisterMountPoint(rootPath, contentPath);
    }

    /// <summary>
    /// Returns whether the specific logical root path is a valid mount point.
    /// </summary>
    public static void RegisterMountPoint(string rootPath)
    {
        UCSPackageNameExtensions.MountPointExists(rootPath);
    }
}

using UnrealSharp.Attributes;
using UnrealSharp.Core.Attributes;
using UnrealSharp.UnrealSharpAsync;
using UnrealSharp.UnrealSharpCore;

namespace UnrealSharp.CoreUObject;

[Binding]
public partial struct FSoftObjectPath
{
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }
        
        return obj.GetType() == GetType() && Equals((FSoftObjectPath)obj);
    }

    public override string ToString()
    {
        return $"{AssetPath.PackageName}.{AssetPath.AssetName}";
    }

    public override int GetHashCode()
    {
        return AssetPath.GetHashCode();
    }
    public void SetPath(FTopLevelAssetPath InPath)
    {
        AssetPath = InPath;
    }
    public bool Valid => AssetPath.Valid;

    public bool Null => AssetPath.Null;
    
    public UObject? Object => UCSSoftObjectPathExtensions.ResolveObject(this);
    
    public static bool operator == (FSoftObjectPath a, FSoftObjectPath b)
    {
        return a.AssetPath == b.AssetPath;
    }

    public static bool operator != (FSoftObjectPath a, FSoftObjectPath b)
    {
        return !(a == b);
    }
}

public static class FSoftObjectPathExtensions
{
    public static Task<UObject> LoadAsync(this FSoftObjectPath softObjectPath)
    {
        return softObjectPath.LoadAsync<UObject>();
    }

    public static async Task<T> LoadAsync<T>(this FSoftObjectPath softObjectPath) where T : UObject
    {
        IReadOnlyList<FSoftObjectPath> loadedPaths = await UCSAsyncLoadSoftPtr.LoadAsync(new List<FSoftObjectPath> { softObjectPath });

        if (loadedPaths.Count == 0 || loadedPaths[0].Object is not T resolved)
        {
            throw new Exception($"Failed to load or cast asset at '{softObjectPath}' to '{typeof(T).Name}'");
        }

        return resolved;
    }

    public static Task<IList<UObject>> LoadAsync(this IList<FSoftObjectPath> softObjectPaths)
    {
        return softObjectPaths.LoadAsync<UObject>();
    }

    public static async Task<IList<T>> LoadAsync<T>(this IList<FSoftObjectPath> softObjectPaths) where T : UObject
    {
        IReadOnlyList<FSoftObjectPath> loadedPaths = await UCSAsyncLoadSoftPtr.LoadAsync(softObjectPaths.AsReadOnly());

        List<T> result = new(loadedPaths.Count);
        foreach (FSoftObjectPath path in loadedPaths)
        {
            if (path.Object is T resolved)
            {
                result.Add(resolved);
            }
        }

        return result;
    }
}
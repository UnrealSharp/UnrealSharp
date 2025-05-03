using UnrealSharp.Core;
using UnrealSharp.CoreUObject;

namespace UnrealSharp.UnrealSharpAsync;

internal static class AsyncLoadUtilities
{
    internal static UObject WorldContextObject
    {
        get
        {
            IntPtr worldContextObject = FCSManagerExporter.CallGetCurrentWorldContext();
            IntPtr worldContextHandle = FCSManagerExporter.CallFindManagedObject(worldContextObject);
            UObject worldContext = GCHandleUtilities.GetObjectFromHandlePtr<UObject>(worldContextHandle)!;
            return worldContext;
        }
    }
}

internal partial class UCSAsyncLoadSoftPtr
{
    private List<FSoftObjectPath> _softObjectPaths = new();
    private readonly Action _onLoadedCompleted;

    private readonly TaskCompletionSource<IReadOnlyList<FSoftObjectPath>> _loadAsyncCompletionSource = new();
    public Task<IReadOnlyList<FSoftObjectPath>> LoadTask => _loadAsyncCompletionSource.Task;

    internal UCSAsyncLoadSoftPtr()
    {
        _onLoadedCompleted = OnLoadedCompleted;
    }
    
    internal static UCSAsyncLoadSoftPtr LoadAsyncSoftPtr(FSoftObjectPath softObjectPath)
    {
        List<FSoftObjectPath> softObjectPaths = new() { softObjectPath };
        return LoadAsyncSoftPtr(softObjectPaths);
    }
    
    internal static UCSAsyncLoadSoftPtr LoadAsyncSoftPtr(List<FSoftObjectPath> softObjectPath)
    {
        UCSAsyncLoadSoftPtr asyncLoadSoftPtr = NewObject<UCSAsyncLoadSoftPtr>(AsyncLoadUtilities.WorldContextObject);
        NativeAsyncUtilities.InitializeAsyncAction(asyncLoadSoftPtr, asyncLoadSoftPtr._onLoadedCompleted);
        
        asyncLoadSoftPtr._softObjectPaths = softObjectPath;
        asyncLoadSoftPtr.LoadSoftObjectPaths(softObjectPath);
        
        return asyncLoadSoftPtr;
    }

    void OnLoadedCompleted()
    {
        _loadAsyncCompletionSource.SetResult(_softObjectPaths);
    }
}

public partial class UCSAsyncLoadPrimaryDataAssets
{
    private IList<FPrimaryAssetId> _primaryAssetIds;
    private readonly Action _onLoadedCompleted;
    
    private readonly TaskCompletionSource<IList<FPrimaryAssetId>> _loadAsyncCompletionSource = new();
    public Task<IList<FPrimaryAssetId>> LoadTask => _loadAsyncCompletionSource.Task;
    
    internal UCSAsyncLoadPrimaryDataAssets()
    {
        _onLoadedCompleted = OnLoadedCompleted;
    }
    
    internal static UCSAsyncLoadPrimaryDataAssets LoadAsyncPrimaryDataAssets(FPrimaryAssetId primaryAssetId, IList<FName>? assetBundles = null)
    {
        List<FPrimaryAssetId> primaryAssetIds = new() { primaryAssetId };
        return LoadAsyncPrimaryDataAssets(primaryAssetIds);
    }
    
    internal static UCSAsyncLoadPrimaryDataAssets LoadAsyncPrimaryDataAssets(IList<FPrimaryAssetId> primaryAssetIds, IList<FName>? assetBundles = null)
    {
        UCSAsyncLoadPrimaryDataAssets asyncLoadPrimaryDataAssets = NewObject<UCSAsyncLoadPrimaryDataAssets>(AsyncLoadUtilities.WorldContextObject);
        NativeAsyncUtilities.InitializeAsyncAction(asyncLoadPrimaryDataAssets, asyncLoadPrimaryDataAssets._onLoadedCompleted);
        
        asyncLoadPrimaryDataAssets._primaryAssetIds = primaryAssetIds;
        
        IList<FName> assetBundlesToLoad = assetBundles ?? new List<FName>();
        asyncLoadPrimaryDataAssets.LoadPrimaryDataAssets(primaryAssetIds, assetBundlesToLoad);
        
        return asyncLoadPrimaryDataAssets;
    }
    
    void OnLoadedCompleted()
    {
        _loadAsyncCompletionSource.SetResult(_primaryAssetIds);
    }
}
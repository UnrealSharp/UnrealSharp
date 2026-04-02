using UnrealSharp.Core;
using UnrealSharp.CoreUObject;
using UnrealSharp.Engine;

namespace UnrealSharp.UnrealSharpAsync;

internal static class AsyncLoadUtilities
{
    internal static UObject WorldContextObject
    {
        get
        {
            IntPtr worldContextObject = FCSManagerExporter.CallGetCurrentWorldContext();
            IntPtr worldContextHandle = FCSManagerExporter.CallFindManagedObject(worldContextObject);
            return GCHandleUtilities.GetObjectFromHandlePtr<UObject>(worldContextHandle)!;
        }
    }
    
    internal static void DisposeAsyncLoadTask<T>(ref TaskCompletionSource<T> tcs)
    {
        if (!tcs.Task.IsCompleted)
        {
            tcs.TrySetCanceled();
        }
        
        tcs.Task.Dispose();
        tcs = null!;
    }
}

internal partial class UCSAsyncLoadSoftPtr
{
    public Task<IReadOnlyList<FSoftObjectPath>> LoadTask => _tcs.Task;

    private IReadOnlyList<FSoftObjectPath> _loadedPaths = null!;
    private TaskCompletionSource<IReadOnlyList<FSoftObjectPath>> _tcs = new();
    private readonly Action _onAsyncCompleted;

    internal UCSAsyncLoadSoftPtr()
    {
        _onAsyncCompleted = OnAsyncCompleted;
    }

    public override void Dispose()
    {
        base.Dispose();
        AsyncLoadUtilities.DisposeAsyncLoadTask(ref _tcs);
    }

    internal static async Task<IReadOnlyList<FSoftObjectPath>> LoadAsync(FSoftObjectPath softObjectPath) => await LoadAsync(new List<FSoftObjectPath> { softObjectPath });
    internal static async Task<IReadOnlyList<FSoftObjectPath>> LoadAsync(IReadOnlyList<FSoftObjectPath> softObjectPaths)
    {
        UWorld world = AsyncLoadUtilities.WorldContextObject.World;
        
        UCSAsyncLoadSoftPtr loader = NewObject<UCSAsyncLoadSoftPtr>(world);
        loader._loadedPaths = softObjectPaths;

        NativeAsyncUtilities.InitializeAsyncAction(loader, loader._onAsyncCompleted);
        loader.LoadSoftObjectPaths(softObjectPaths.ToList());

        return await loader._tcs.Task.ConfigureWithUnrealContext();
    }

    private void OnAsyncCompleted()
    {
        _tcs.TrySetResult(_loadedPaths);
    }
}

internal partial class UCSAsyncLoadPrimaryDataAssets
{
    public Task<IList<FPrimaryAssetId>> LoadTask => _tcs.Task;

    private IList<FPrimaryAssetId> _loadedIds = null!;
    private TaskCompletionSource<IList<FPrimaryAssetId>> _tcs = new();
    private readonly Action _onAsyncCompleted;

    internal UCSAsyncLoadPrimaryDataAssets()
    {
        _onAsyncCompleted = OnAsyncCompleted;
    }

    public override void Dispose()
    {
        base.Dispose();
        AsyncLoadUtilities.DisposeAsyncLoadTask(ref _tcs);
    }

    internal static async Task<IList<FPrimaryAssetId>> LoadAsync(FPrimaryAssetId primaryAssetId, IList<FName>? assetBundles = null) => await LoadAsync(new List<FPrimaryAssetId> { primaryAssetId }, assetBundles);
    internal static async Task<IList<FPrimaryAssetId>> LoadAsync(IList<FPrimaryAssetId> primaryAssetIds, IList<FName>? assetBundles = null)
    {
        UWorld world = AsyncLoadUtilities.WorldContextObject.World;
        
        UCSAsyncLoadPrimaryDataAssets loader = NewObject<UCSAsyncLoadPrimaryDataAssets>(world);
        loader._loadedIds = primaryAssetIds;

        NativeAsyncUtilities.InitializeAsyncAction(loader, loader._onAsyncCompleted);

        IList<FName> bundles = assetBundles ?? new List<FName>();
        loader.LoadPrimaryDataAssets(primaryAssetIds, bundles);

        return await loader._tcs.Task.ConfigureWithUnrealContext();
    }

    private void OnAsyncCompleted()
    {
        _tcs.TrySetResult(_loadedIds);
    }
}
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
            return GCHandleUtilities.GetObjectFromHandlePtr<UObject>(worldContextHandle)!;
        }
    }
}

internal partial class UCSAsyncLoadSoftPtr
{
    public Task<IReadOnlyList<FSoftObjectPath>> LoadTask => _tcs.Task;

    private List<FSoftObjectPath> _loadedPaths = null!;
    private readonly TaskCompletionSource<IReadOnlyList<FSoftObjectPath>> _tcs = new();
    private readonly Action _onAsyncCompleted;

    internal UCSAsyncLoadSoftPtr()
    {
        _onAsyncCompleted = OnAsyncCompleted;
    }

    internal static async Task<IReadOnlyList<FSoftObjectPath>> LoadAsync(FSoftObjectPath softObjectPath) =>
        await LoadAsync([softObjectPath]);

    internal static async Task<IReadOnlyList<FSoftObjectPath>> LoadAsync(List<FSoftObjectPath> softObjectPaths)
    {
        UCSAsyncLoadSoftPtr loader = NewObject<UCSAsyncLoadSoftPtr>(AsyncLoadUtilities.WorldContextObject);
        loader._loadedPaths = softObjectPaths;

        NativeAsyncUtilities.InitializeAsyncAction(loader, loader._onAsyncCompleted);

        loader.LoadSoftObjectPaths(softObjectPaths);
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
    private readonly TaskCompletionSource<IList<FPrimaryAssetId>> _tcs = new();
    private readonly Action _onAsyncCompleted;

    internal UCSAsyncLoadPrimaryDataAssets()
    {
        _onAsyncCompleted = OnAsyncCompleted;
    }

    internal static async Task<IList<FPrimaryAssetId>> LoadAsync(FPrimaryAssetId primaryAssetId, IList<FName>? assetBundles = null)
        => await LoadAsync(new List<FPrimaryAssetId> { primaryAssetId }, assetBundles);

    internal static async Task<IList<FPrimaryAssetId>> LoadAsync(IList<FPrimaryAssetId> primaryAssetIds, IList<FName>? assetBundles = null)
    {
        UCSAsyncLoadPrimaryDataAssets loader = NewObject<UCSAsyncLoadPrimaryDataAssets>(AsyncLoadUtilities.WorldContextObject);
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
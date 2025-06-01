using UnrealSharp.Core;
using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;
using UnrealSharp.UnrealSharpAsync;

namespace UnrealSharp.Engine;

public partial class UAssetManager
{
    /// <summary>
    ///     Gets the AssetManager singleton of the specified type
    /// </summary>
    public static T Get<T>() where T : UAssetManager
    {
        IntPtr handle = UAssetManagerExporter.CallGetAssetManager();

        if (handle == IntPtr.Zero)
        {
            throw new Exception("Failed to get AssetManager singleton");
        }

        return GCHandleUtilities.GetObjectFromHandlePtr<T>(handle)!;
    }

    /// <summary>
    ///     Gets the AssetManager singleton
    /// </summary>
    public static UAssetManager Get()
    {
        return Get<UAssetManager>();
    }

    /// <summary>
    ///     Loads a primary asset by its FPrimaryAssetId and returns it as a UObject.
    ///     Optionally, you can provide a list of bundles to load alongside the asset.
    ///     This method calls the generic LoadPrimaryAsset<T> method with UObject as the type.
    /// </summary>
    /// <param name="primaryAsset">The primary asset to load</param>
    /// <param name="bundles">The bundles to load along with the asset (optional)</param>
    /// <returns>The loaded UObject</returns>
    public async Task<UObject> LoadPrimaryAsset(FPrimaryAssetId primaryAsset, IList<FName>? bundles = null)
    {
        return await LoadPrimaryAsset<UObject>(primaryAsset, bundles);
    }

    /// <summary>
    ///     Loads a primary asset of a specified type (T) by its FPrimaryAssetId.
    ///     Optionally, you can provide a list of bundles to load alongside the asset.
    ///     If the asset cannot be loaded or is not of the expected type, an exception will be thrown.
    /// </summary>
    /// <param name="primaryAsset">The primary asset to load</param>
    /// <param name="bundles">The bundles to load along with the asset (optional)</param>
    /// <typeparam name="T">The type of the asset to load</typeparam>
    /// <returns>The loaded asset of type T</returns>
    public async Task<T> LoadPrimaryAsset<T>(FPrimaryAssetId primaryAsset, IList<FName>? bundles = null) where T : UObject
    {
        IList<T> assets = await LoadPrimaryAssets<T>(new List<FPrimaryAssetId> { primaryAsset }, bundles);
        
        if (assets.Count == 0)
        {
            throw new Exception($"Failed to load {primaryAsset}");
        }

        return assets[0];
    }

    /// <summary>
    ///     Loads multiple primary assets by their FPrimaryAssetId list and returns them as UObjects.
    ///     Optionally, you can provide a list of bundles to load alongside the assets.
    /// </summary>
    /// <param name="primaryAssets">A list of primary assets to load</param>
    /// <param name="bundles">The bundles to load along with the assets (optional)</param>
    /// <returns>A list of loaded UObjects</returns>
    public async Task<IList<UObject>> LoadPrimaryAssets(IList<FPrimaryAssetId> primaryAssets, IList<FName>? bundles = null)
    {
        return await LoadPrimaryAssets<UObject>(primaryAssets, bundles);
    }

    /// <summary>
    ///     Loads multiple primary assets of a specified type (T) by their FPrimaryAssetId list.
    ///     Optionally, you can provide a list of bundles to load alongside the assets.
    ///     If the assets cannot be loaded or are not of the expected type, an exception will be thrown.
    /// </summary>
    /// <param name="primaryAssets">A list of primary assets to load</param>
    /// <param name="bundles">The bundles to load along with the assets (optional)</param>
    /// <typeparam name="T">The type of the assets to load</typeparam>
    /// <returns>A list of loaded assets of type T</returns>
    public async Task<IList<T>> LoadPrimaryAssets<T>(IList<FPrimaryAssetId> primaryAssets, IList<FName>? bundles = null) where T : UObject
    {
        IList<UObject> loadedAssets = await LoadAssetsInternal<T>(primaryAssets, bundles);
        return loadedAssets.Cast<T>().ToList();
    }

    /// <summary>
    ///     Loads a primary asset class by its FPrimaryAssetId and returns it as a TSubclassOf
    ///     <UObject>
    ///         .
    ///         Optionally, you can provide a list of bundles to load alongside the asset.
    ///         This method calls the generic LoadPrimaryAssetClass<T> method with UObject as the type.
    /// </summary>
    /// <param name="primaryAsset">The primary asset to load</param>
    /// <param name="bundles">The bundles to load along with the asset (optional)</param>
    /// <returns>The loaded TSubclassOf<UObject> asset class</returns>
    public async Task<TSubclassOf<UObject>> LoadPrimaryAssetClass(FPrimaryAssetId primaryAsset, IList<FName>? bundles = null)
    {
        return await LoadPrimaryAssetClass<UObject>(primaryAsset, bundles);
    }

    /// <summary>
    ///     Loads a primary asset class of a specified type (T) by its FPrimaryAssetId.
    ///     Optionally, you can provide a list of bundles to load alongside the asset.
    ///     If the asset class cannot be loaded or is not of the expected type, an exception will be thrown.
    /// </summary>
    /// <param name="primaryAsset">The primary asset to load</param>
    /// <param name="bundles">The bundles to load along with the asset (optional)</param>
    /// <typeparam name="T">The type of the asset class to load</typeparam>
    /// <returns>The loaded TSubclassOf<T> asset class</returns>
    public async Task<TSubclassOf<T>> LoadPrimaryAssetClass<T>(FPrimaryAssetId primaryAsset, IList<FName>? bundles = null) where T : UObject
    {
        IList<TSubclassOf<T>> assets = await LoadPrimaryAssetClasses<T>(new List<FPrimaryAssetId> { primaryAsset }, bundles);

        if (assets.Count == 0)
        {
            throw new Exception($"Failed to load {primaryAsset}");
        }

        return assets[0];
    }

    /// <summary>
    ///     Loads multiple primary asset classes by their FPrimaryAssetId list and returns them as TSubclassOf.
    /// </summary>
    /// <param name="primaryAssets">A list of primary assets to load</param>
    /// <param name="bundles">The bundles to load along with the assets (optional)</param>
    /// <returns>A list of loaded TSubclassOf<UObject> asset classes</returns>
    public async Task<IList<TSubclassOf<UObject>>> LoadPrimaryAssetClasses(IList<FPrimaryAssetId> primaryAssets, IList<FName>? bundles = null)
    {
        return await LoadPrimaryAssetClasses<UObject>(primaryAssets, bundles);
    }

    /// <summary>
    ///     Loads multiple primary asset classes of a specified type (T) by their FPrimaryAssetId list.
    ///     Optionally, you can provide a list of bundles to load alongside the assets.
    ///     If the assets cannot be loaded or are not of the expected type, an exception will be thrown.
    /// </summary>
    /// <param name="primaryAssets">A list of primary assets to load</param>
    /// <param name="bundles">The bundles to load along with the assets (optional)</param>
    /// <typeparam name="T">The type of the asset classes to load</typeparam>
    /// <returns>A list of loaded asset classes of type T</returns>
    public async Task<IList<TSubclassOf<T>>> LoadPrimaryAssetClasses<T>(IList<FPrimaryAssetId> primaryAssets, IList<FName>? bundles = null) where T : UObject
    {
        IList<UObject> loadedAssets = await LoadAssetsInternal<T>(primaryAssets, bundles);
        return loadedAssets.Select(asset => new TSubclassOf<T>(asset.NativeObject)).ToList();
    }

    private async Task<IList<UObject>> LoadAssetsInternal<T>(IList<FPrimaryAssetId> primaryAssets, IList<FName>? bundles = null) where T : UObject
    {
        IList<FPrimaryAssetId> loadedAssets = await UCSAsyncLoadPrimaryDataAssets.LoadAsync(primaryAssets, bundles);

        List<UObject> loadedObjects = new(loadedAssets.Count);
        foreach (FPrimaryAssetId assetId in loadedAssets)
        {
            if (assetId.AssetClass.Value.Valid)
            {
                var loaded = SystemLibrary.GetClass(assetId);

                if (!loaded.IsChildOf(typeof(T)))
                {
                     throw new InvalidCastException($"Asset '{assetId}' could not be cast to '{typeof(T).Name}' or was not found.");
                }
                
                loadedObjects.Add(loaded);
            }
            else if (assetId.Asset)
            {
                var loaded = SystemLibrary.GetObject(assetId);
                
                if (loaded is not T typedObject)
                {
                    throw new InvalidCastException($"Asset '{assetId}' could not be cast to '{typeof(T).Name}' or was not found.");
                }
                
                loadedObjects.Add(typedObject);
            }
        }

        return loadedObjects;
    }

    /// <summary>
    ///     Returns the Object associated with a Primary Asset Id, this will only return a valid object if it is in memory, it
    ///     will not load it
    /// </summary>
    /// <param name="primaryAssetId">The Primary Asset Id to get the object for</param>
    /// <returns>The object associated with the Primary Asset Id, or null if it is not loaded</returns>
    public UObject? GetPrimaryAssetObject(FPrimaryAssetId primaryAssetId)
    {
        return SystemLibrary.GetObject(primaryAssetId);
    }

    /// <summary>
    ///     Returns the Object associated with a Primary Asset Id, this will only return a valid object if it is in memory, it
    ///     will not load it
    /// </summary>
    /// <param name="primaryAssetId">The Primary Asset Id to get the object for</param>
    /// <typeparam name="T">The type of object to return</typeparam>
    public T? GetPrimaryAssetObject<T>(FPrimaryAssetId primaryAssetId) where T : UObject
    {
        var foundObject = SystemLibrary.GetObject(primaryAssetId);
        return foundObject as T;
    }

    /// <summary>
    ///     Returns the Blueprint Class associated with a Primary Asset Id, this will only return a valid object if it is in
    ///     memory, it will not load it
    /// </summary>
    /// <param name="primaryAssetId">The Primary Asset Id to get the object for</param>
    /// <returns>The Blueprint Class associated with the Primary Asset Id, or null if it is not loaded</returns>
    public TSubclassOf<UObject> GetPrimaryAssetObjectClass(FPrimaryAssetId primaryAssetId)
    {
        return SystemLibrary.GetClass(primaryAssetId);
    }

    /// <summary>
    ///     Returns the Object Id associated with a Primary Asset Id, this works even if the asset is not loaded
    /// </summary>
    /// <param name="primaryAssetId">The Primary Asset Id to get the object for</param>
    /// <returns>The Object Id associated with the Primary Asset Id</returns>
    public TSoftObjectPtr<UObject> GetSoftObjectReferenceFromPrimaryAssetId(FPrimaryAssetId primaryAssetId)
    {
        return SystemLibrary.GetSoftObjectReference(primaryAssetId);
    }

    /// <summary>
    ///     Returns the Blueprint Class Id associated with a Primary Asset Id, this works even if the asset is not loaded
    /// </summary>
    /// <param name="primaryAssetId">The Primary Asset Id to get the object for</param>
    /// <returns>The Blueprint Class Id associated with the Primary Asset Id</returns>
    public TSoftClassPtr<UClass> GetSoftClassReferenceFromPrimaryAssetId(FPrimaryAssetId primaryAssetId)
    {
        return SystemLibrary.GetSoftClassReference(primaryAssetId);
    }

    /// <summary>
    ///     Returns the Primary Asset Id for an Object, this can return an invalid one if not registered
    /// </summary>
    /// <param name="obj">The object to get the Primary Asset Id for</param>
    /// <returns>The Primary Asset Id for the object</returns>
    public FPrimaryAssetId GetPrimaryAssetIdFromObject(UObject obj)
    {
        return SystemLibrary.GetPrimaryAssetIdFromObject(obj);
    }

    /// <summary>
    ///     Returns the Primary Asset Id for a Class, this can return an invalid one if not registered
    /// </summary>
    /// <param name="class">The class to get the Primary Asset Id for</param>
    /// <returns>The Primary Asset Id for the class</returns>
    public FPrimaryAssetId GetPrimaryAssetIdFromClass(TSubclassOf<UObject> @class)
    {
        return SystemLibrary.GetPrimaryAssetIdFromClass(@class);
    }

    /// <summary>
    ///     Returns the Primary Asset Id for a Soft Object Reference, this can return an invalid one if not registered
    /// </summary>
    /// <param name="softObjectReference">The soft object reference to get the Primary Asset Id for</param>
    /// <returns>The Primary Asset Id for the soft object reference</returns>
    public FPrimaryAssetId GetPrimaryAssetIdForPath(TSoftObjectPtr<UObject> softObjectReference)
    {
        return SystemLibrary.GetPrimaryAssetIdFromSoftObjectReference(softObjectReference);
    }

    /// <summary>
    ///     Returns list of PrimaryAssetIds for a PrimaryAssetType
    /// </summary>
    /// <param name="assetType">The type of primary asset to get the list for</param>
    /// <param name="outPrimaryAssetIds">The list of primary asset ids</param>
    public void GetPrimaryAssetIdList(FPrimaryAssetType assetType, out IList<FPrimaryAssetId> outPrimaryAssetIds)
    {
        SystemLibrary.GetPrimaryAssetIdList(assetType, out outPrimaryAssetIds);
    }

    /// <summary>
    ///     Unloads a primary asset, which allows it to be garbage collected if nothing else is referencing it
    /// </summary>
    /// <param name="primaryAsset">The primary asset to unload</param>
    /// <returns>True if the asset was successfully unloaded</returns>
    public void UnloadPrimaryAsset(FPrimaryAssetId primaryAsset)
    {
        SystemLibrary.Unload(primaryAsset);
    }

    /// <summary>
    ///     Unloads a primary asset, which allows it to be garbage collected if nothing else is referencing it
    /// </summary>
    /// <param name="primaryAssets">The primary assets to unload</param>
    public void UnloadPrimaryAssets(IList<FPrimaryAssetId> primaryAssets)
    {
        SystemLibrary.UnloadPrimaryAssetList(primaryAssets);
    }

    /// <summary>
    ///     Returns the list of loaded bundles for a given Primary Asset. This will return false if the asset is not loaded at
    ///     all.
    ///     If ForceCurrentState is true it will return the current state even if a load is in process
    /// </summary>
    /// <param name="primaryAssetId">The primary asset to get the bundle state for</param>
    /// <param name="bForceCurrentState">Whether to force the current state</param>
    /// <param name="outBundles">The list of bundles that are loaded</param>
    /// <returns>True if the asset is loaded and the list of bundles is valid</returns>
    public bool GetCurrentBundleState(FPrimaryAssetId primaryAssetId, bool bForceCurrentState, out IList<FName> outBundles)
    {
        return SystemLibrary.GetCurrentBundleState(primaryAssetId, bForceCurrentState, out outBundles);
    }
}
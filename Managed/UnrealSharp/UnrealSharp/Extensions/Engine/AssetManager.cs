using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;

namespace UnrealSharp.Engine;

public partial class UAssetManager
{
    /// <summary>
    /// Gets the AssetManager singleton of the specified type
    /// </summary>
    public static T Get<T>() where T : UAssetManager
    {
        IntPtr handle = UAssetManagerExporter.CallGetAssetManager();
        
        if (handle == IntPtr.Zero)
        {
            throw new Exception("Failed to get AssetManager singleton");
        }
        
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle)!;
    }
    
    /// <summary>
    /// Gets the AssetManager singleton
    /// </summary>
    public static UAssetManager Get()
    {
        return Get<UAssetManager>();
    }
    
    /// <summary>
    /// Load a primary asset object into memory, this will cause it to stay loaded until it is explicitly unloaded.
    /// The completed event will happen when the load succeeds or fails, you should cast the Loaded object to verify it is the correct type.
    /// If LoadBundles is specified, those bundles are loaded along with the asset.
    /// </summary>
    /// <param name="primaryAsset">The primary asset to load</param>
    /// <param name="bundles">The bundles to load along with the asset</param>
    /// <param name="onLoaded">The callback to execute when the asset is loaded</param>
    public void LoadPrimaryAsset(FPrimaryAssetId primaryAsset, IList<FName> bundles, OnPrimaryAssetLoaded onLoaded)
    {
        UAsyncActionLoadPrimaryAsset loadPrimaryAsset = UAsyncActionLoadPrimaryAsset.AsyncLoadPrimaryAsset(WorldContextObject, primaryAsset, bundles);
        loadPrimaryAsset.Completed += onLoaded;
        loadPrimaryAsset.Activate();
    }
    
    /// <summary>
    /// Load a primary asset object into memory, this will cause it to stay loaded until it is explicitly unloaded.
    /// The completed event will happen when the load succeeds or fails, you should cast the Loaded object to verify it is the correct type.
    /// If LoadBundles is specified, those bundles are loaded along with the asset.
    /// </summary>
    /// <param name="primaryAsset">The primary asset to load</param>
    /// <param name="onLoaded">The callback to execute when the asset is loaded</param>
    public void LoadPrimaryAsset(FPrimaryAssetId primaryAsset, OnPrimaryAssetLoaded onLoaded)
    {
        LoadPrimaryAsset(primaryAsset, new List<FName>(), onLoaded);
    }
    
    /// <summary>
    /// Load a list of primary asset objects into memory, this will cause them to stay loaded until explicitly unloaded.
    /// The completed event will happen when the load succeeds or fails, and the Loaded list will contain all of the requested assets found at completion.
    /// If LoadBundles is specified, those bundles are loaded along with the assets.
    /// </summary>
    /// <param name="primaryAssets">The primary assets to load</param>
    /// <param name="bundles">The bundles to load along with the assets</param>
    /// <param name="onLoaded">The callback to execute when the assets are loaded</param>
    public void LoadPrimaryAssets(IList<FPrimaryAssetId> primaryAssets, IList<FName> bundles, OnPrimaryAssetListLoaded onLoaded)
    {
        UAsyncActionLoadPrimaryAssetList loadPrimaryAssets = UAsyncActionLoadPrimaryAssetList.AsyncLoadPrimaryAssetList(WorldContextObject, primaryAssets, bundles);
        loadPrimaryAssets.Completed += onLoaded;
        loadPrimaryAssets.Activate();
    }
    
    /// <summary>
    /// Load a list of primary asset objects into memory, this will cause them to stay loaded until explicitly unloaded.
    /// The completed event will happen when the load succeeds or fails, and the Loaded list will contain all of the requested assets found at completion.
    /// If LoadBundles is specified, those bundles are loaded along with the assets.
    /// </summary>
    /// <param name="primaryAssets">The primary assets to load</param>
    /// <param name="onLoaded">The callback to execute when the assets are loaded</param>
    public void LoadPrimaryAssets(IList<FPrimaryAssetId> primaryAssets, OnPrimaryAssetListLoaded onLoaded)
    {
        LoadPrimaryAssets(primaryAssets, new List<FName>(), onLoaded);
    }
    
    /// <summary>
    /// Load a primary asset class  into memory, this will cause it to stay loaded until it is explicitly unloaded.
    /// The completed event will happen when the load succeeds or fails, you should cast the Loaded class to verify it is the correct type.
    /// If LoadBundles is specified, those bundles are loaded along with the asset.
    /// </summary>
    /// <param name="primaryAsset">The primary asset to load</param>
    /// <param name="bundles">The bundles to load along with the asset</param>
    /// <param name="onLoaded">The callback to execute when the asset is loaded</param>
    public void LoadPrimaryAssetClass(FPrimaryAssetId primaryAsset, IList<FName> bundles, OnPrimaryAssetClassLoaded onLoaded)
    {
        UAsyncActionLoadPrimaryAssetClass loadPrimaryAssetClass = UAsyncActionLoadPrimaryAssetClass.AsyncLoadPrimaryAssetClass(WorldContextObject, primaryAsset, bundles);
        loadPrimaryAssetClass.Completed += onLoaded;
        loadPrimaryAssetClass.Activate();
    }
    
    /// <summary>
    /// Load a primary asset class  into memory, this will cause it to stay loaded until it is explicitly unloaded.
    /// The completed event will happen when the load succeeds or fails, you should cast the Loaded class to verify it is the correct type.
    /// If LoadBundles is specified, those bundles are loaded along with the asset.
    /// </summary>
    /// <param name="primaryAsset">The primary asset to load</param>
    /// <param name="onLoaded">The callback to execute when the asset is loaded</param>
    public void LoadPrimaryAssetClass(FPrimaryAssetId primaryAsset, OnPrimaryAssetClassLoaded onLoaded)
    {
        LoadPrimaryAssetClass(primaryAsset, new List<FName>(), onLoaded);
    }
    
    /// <summary>
    /// Load a list of primary asset classes into memory, this will cause them to stay loaded until explicitly unloaded.
    /// The completed event will happen when the load succeeds or fails, and the Loaded list will contain all of the requested classes found at completion.
    /// If LoadBundles is specified, those bundles are loaded along with the assets.
    /// </summary>
    /// <param name="primaryAssets">The primary assets to load</param>
    /// <param name="bundles">The bundles to load along with the assets</param>
    /// <param name="onLoaded">The callback to execute when the classes are loaded</param>
    public void LoadPrimaryAssetClasses(IList<FPrimaryAssetId> primaryAssets, IList<FName> bundles, OnPrimaryAssetClassListLoaded onLoaded)
    {
        UAsyncActionLoadPrimaryAssetClassList loadPrimaryAssetClasses = UAsyncActionLoadPrimaryAssetClassList.AsyncLoadPrimaryAssetClassList(WorldContextObject, primaryAssets, bundles);
        loadPrimaryAssetClasses.Completed += onLoaded;
        loadPrimaryAssetClasses.Activate();
    }
    
    /// <summary>
    /// Load a list of primary asset classes into memory, this will cause them to stay loaded until explicitly unloaded.
    /// The completed event will happen when the load succeeds or fails, and the Loaded list will contain all of the requested classes found at completion.
    /// If LoadBundles is specified, those bundles are loaded along with the assets.
    /// </summary>
    /// <param name="primaryAssets">The primary assets to load</param>
    /// <param name="onLoaded">The callback to execute when the classes are loaded</param>
    public void LoadPrimaryAssetClasses(IList<FPrimaryAssetId> primaryAssets, OnPrimaryAssetClassListLoaded onLoaded)
    {
        LoadPrimaryAssetClasses(primaryAssets, new List<FName>(), onLoaded);
    }
    
    /// <summary>
    /// Returns the Object associated with a Primary Asset Id, this will only return a valid object if it is in memory, it will not load it
    /// </summary>
    /// <param name="primaryAssetId">The Primary Asset Id to get the object for</param>
    /// <returns>The object associated with the Primary Asset Id, or null if it is not loaded</returns>
    public UObject? GetPrimaryAssetObject(FPrimaryAssetId primaryAssetId)
    {
        return SystemLibrary.GetObject(primaryAssetId);
    }
    
    /// <summary>
    /// Returns the Object associated with a Primary Asset Id, this will only return a valid object if it is in memory, it will not load it
    /// </summary>
    /// <param name="primaryAssetId">The Primary Asset Id to get the object for</param>
    /// <typeparam name="T">The type of object to return</typeparam>
    public T? GetPrimaryAssetObject<T>(FPrimaryAssetId primaryAssetId) where T : UObject
    {
        UObject foundObject = SystemLibrary.GetObject(primaryAssetId);
        return foundObject as T;
    }
    
    /// <summary>
    /// Returns the Blueprint Class associated with a Primary Asset Id, this will only return a valid object if it is in memory, it will not load it
    /// </summary>
    /// <param name="primaryAssetId">The Primary Asset Id to get the object for</param>
    /// <returns>The Blueprint Class associated with the Primary Asset Id, or null if it is not loaded</returns>
    public TSubclassOf<UObject> GetPrimaryAssetObjectClass(FPrimaryAssetId primaryAssetId)
    {
        return SystemLibrary.GetClass(primaryAssetId);
    }
    
    /// <summary>
    /// Returns the Object Id associated with a Primary Asset Id, this works even if the asset is not loaded
    /// </summary>
    /// <param name="primaryAssetId">The Primary Asset Id to get the object for</param>
    /// <returns>The Object Id associated with the Primary Asset Id</returns>
    public TSoftObjectPtr<UObject> GetSoftObjectReferenceFromPrimaryAssetId(FPrimaryAssetId primaryAssetId)
    {
        return SystemLibrary.GetSoftObjectReference(primaryAssetId);
    }
    
    /// <summary>
    /// Returns the Blueprint Class Id associated with a Primary Asset Id, this works even if the asset is not loaded
    /// </summary>
    /// <param name="primaryAssetId">The Primary Asset Id to get the object for</param>
    /// <returns>The Blueprint Class Id associated with the Primary Asset Id</returns>
    public TSoftClassPtr<UClass> GetSoftClassReferenceFromPrimaryAssetId(FPrimaryAssetId primaryAssetId)
    {
        return SystemLibrary.GetSoftClassReference(primaryAssetId);
    }
    
    /// <summary>
    /// Returns the Primary Asset Id for an Object, this can return an invalid one if not registered
    /// </summary>
    /// <param name="obj">The object to get the Primary Asset Id for</param>
    /// <returns>The Primary Asset Id for the object</returns>
    public FPrimaryAssetId GetPrimaryAssetIdFromObject(UObject obj)
    {
        return SystemLibrary.GetPrimaryAssetIdFromObject(obj);
    }

    /// <summary>
    /// Returns the Primary Asset Id for a Class, this can return an invalid one if not registered
    /// </summary>
    /// <param name="class">The class to get the Primary Asset Id for</param>
    /// <returns>The Primary Asset Id for the class</returns>
    public FPrimaryAssetId GetPrimaryAssetIdFromClass(TSubclassOf<UObject> @class)
    {
        return SystemLibrary.GetPrimaryAssetIdFromClass(@class);
    }
    
    /// <summary>
    /// Returns the Primary Asset Id for a Soft Object Reference, this can return an invalid one if not registered
    /// </summary>
    /// <param name="softObjectReference">The soft object reference to get the Primary Asset Id for</param>
    /// <returns>The Primary Asset Id for the soft object reference</returns>
    public FPrimaryAssetId GetPrimaryAssetIdForPath(TSoftObjectPtr<UObject> softObjectReference)
    {
        return SystemLibrary.GetPrimaryAssetIdFromSoftObjectReference(softObjectReference);
    }
    
    /// <summary>
    /// Returns list of PrimaryAssetIds for a PrimaryAssetType
    /// </summary>
    /// <param name="assetType">The type of primary asset to get the list for</param>
    /// <param name="outPrimaryAssetIds">The list of primary asset ids</param>
    public void GetPrimaryAssetIdList(FPrimaryAssetType assetType, out IList<FPrimaryAssetId> outPrimaryAssetIds)
    {
        SystemLibrary.GetPrimaryAssetIdList(assetType, out outPrimaryAssetIds);
    }
    
    /// <summary>
    /// Unloads a primary asset, which allows it to be garbage collected if nothing else is referencing it
    /// </summary>
    /// <param name="primaryAsset">The primary asset to unload</param>
    /// <returns>True if the asset was successfully unloaded</returns>
    public void UnloadPrimaryAsset(FPrimaryAssetId primaryAsset)
    {
        SystemLibrary.Unload(primaryAsset);
    }
    
    /// <summary>
    /// Unloads a primary asset, which allows it to be garbage collected if nothing else is referencing it
    /// </summary>
    /// <param name="primaryAssets">The primary assets to unload</param>
    public void UnloadPrimaryAssets(IList<FPrimaryAssetId> primaryAssets)
    {
        SystemLibrary.UnloadPrimaryAssetList(primaryAssets);
    }

    /// <summary>
    /// Returns the list of loaded bundles for a given Primary Asset. This will return false if the asset is not loaded at all.
    /// If ForceCurrentState is true it will return the current state even if a load is in process
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
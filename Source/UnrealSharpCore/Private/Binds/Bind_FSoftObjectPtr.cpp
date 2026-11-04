#include "CSBindsRegistry.h"
#include "CSManager.h"
#include "UObject/SoftObjectPtr.h"

DECLARE_UNREALSHARP_BINDER(Bind_FSoftObjectPtr)
{
	void* LoadSynchronous(const TSoftObjectPtr<UObject>* SoftObjectPtr)
	{
		if (SoftObjectPtr->IsNull())
		{
			return nullptr;
		}
	
		UObject* LoadedObject = SoftObjectPtr->LoadSynchronous();
		return UCSManager::Get().FindManagedObject(LoadedObject);
	}
	
	BIND_UNREALSHARP_FUNCTION(LoadSynchronous)

    FSoftObjectPath MakePath(const FTopLevelAssetPath* AssetPath, const UTF8CHAR* Text, int32 Length)
    {
        if (Length == 0)
        {
            return FSoftObjectPath(*AssetPath);
        }
        return FSoftObjectPath(*AssetPath, FUtf8String::ConstructFromPtrSize(Text, Length));
    }

    const FSoftObjectPath* ReadSoft(const FSoftObjectPtr* Source, int32 Index)
    {
        return &Source[Index].ToSoftObjectPath();
    }

    FSoftObjectPath* WriteSoft(FSoftObjectPtr* Destination, int32 Index)
    {
        Destination[Index].ResetWeakPtr();
        return &Destination[Index].GetUniqueID();
    }

    void FromObject(const UObject* Object, FTopLevelAssetPath* AssetPath, FUtf8String* SubPath)
    {
        const FSoftObjectPath Path(Object);
        *AssetPath = Path.GetAssetPath();
        *SubPath = Path.GetSubPathUtf8String();
    }

    UObject* Resolve(FWeakObjectPtr* CachedObject, const FTopLevelAssetPath* AssetPath, const UTF8CHAR* Text, int32 Length, uint8 Load)
    {
        bool UseCache = true;
#if WITH_EDITOR
        UseCache = UE::GetPlayInEditorID() == INDEX_NONE;
#endif
        if (UseCache)
        {
            if (UObject* Object = CachedObject->Get())
            {
                return Object;
            }
        }
        if (AssetPath->IsNull())
        {
            return nullptr;
        }
        const FSoftObjectPath Path = MakePath(AssetPath, Text, Length);
        UObject* Resolved = Load ? Path.TryLoad() : Path.ResolveObject();
        // Match TPersistentObjectPtr: do not cache objects still being async loaded.
        if (UseCache && !(IsInAsyncLoadingThread() && Resolved && Resolved->HasAnyInternalFlags(EInternalObjectFlags_AsyncLoading)))
        {
            *CachedObject = Resolved;
        }
        return ::GetValid(Resolved);
    }

    void* Get(FWeakObjectPtr* CachedObject, const FTopLevelAssetPath* AssetPath, const UTF8CHAR* Text, int32 Length)
    {
        return UCSManager::Get().FindManagedObject(Resolve(CachedObject, AssetPath, Text, Length, 0));
    }

    int32 PathHash(const FTopLevelAssetPath* AssetPath, const UTF8CHAR* Text, int32 Length)
    {
        return GetTypeHash(MakePath(AssetPath, Text, Length));
    }

    BIND_UNREALSHARP_FUNCTION(Get)
    BIND_UNREALSHARP_FUNCTION(ReadSoft)
    BIND_UNREALSHARP_FUNCTION(WriteSoft)
    BIND_UNREALSHARP_FUNCTION(FromObject)
    BIND_UNREALSHARP_FUNCTION(Resolve)
    BIND_UNREALSHARP_FUNCTION(PathHash)
}

#include "CSManager.h"
#include "Engine/AssetManager.h"

DECLARE_UNREALSHARP_BINDER(Bind_UAssetManager)
{
	void* GetAssetManager()
	{
		UAssetManager& AssetManager = UAssetManager::Get();
		return UCSManager::Get().FindManagedObject(&AssetManager);
	}
	
	BIND_UNREALSHARP_FUNCTION(GetAssetManager)
}

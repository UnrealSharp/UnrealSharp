#include "CSManager.h"
#include "Engine/AssetManager.h"

DECLARE_UNREALSHARP_EXPORTER(UAssetManagerExporter)
{
	void* GetAssetManager()
	{
		UAssetManager& AssetManager = UAssetManager::Get();
		return UCSManager::Get().FindManagedObject(&AssetManager);
	}
	
	EXPORT_UNREALSHARP_FUNCTION(GetAssetManager)
}

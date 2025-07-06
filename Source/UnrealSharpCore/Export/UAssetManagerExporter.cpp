#include "UAssetManagerExporter.h"
#include "CSManager.h"
#include "Engine/AssetManager.h"

void* UUAssetManagerExporter::GetAssetManager()
{
	UAssetManager& AssetManager = UAssetManager::Get();
	return UCSManager::Get().FindManagedObject(&AssetManager);
}

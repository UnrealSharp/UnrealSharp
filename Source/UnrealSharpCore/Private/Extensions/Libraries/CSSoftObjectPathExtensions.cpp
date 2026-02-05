#include "Extensions/Libraries/CSSoftObjectPathExtensions.h"
#include "Engine/AssetManager.h"

UObject* UCSSoftObjectPathExtensions::ResolveObject(const FSoftObjectPath& SoftObjectPath)
{
	return SoftObjectPath.ResolveObject();
}

FPrimaryAssetId UCSSoftObjectPathExtensions::GetPrimaryAssetId(const FSoftObjectPath& SoftObjectPath)
{
	return UAssetManager::Get().GetPrimaryAssetIdForPath(SoftObjectPath);
}

#include "Extensions/Libraries/CSSoftObjectPathExtensions.h"
#include "Engine/AssetManager.h"

UObject* UCSSoftObjectPathExtensions::ResolveObject(const FSoftObjectPath& SoftObjectPath)
{
	return SoftObjectPath.ResolveObject();
}

FPrimaryAssetId UCSSoftObjectPathExtensions::GetPrimaryAssetId_Internal(const FSoftObjectPath& SoftObjectPath)
{
	return UAssetManager::Get().GetPrimaryAssetIdForPath(SoftObjectPath);
}

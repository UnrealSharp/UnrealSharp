#include "CSPrimaryDataAsset.h"

FPrimaryAssetId UCSPrimaryDataAsset::GetPrimaryAssetId() const
{
	return FPrimaryAssetId(AssetName, GetFName());
}

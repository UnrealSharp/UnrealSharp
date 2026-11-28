#include "Extensions/Libraries/CSSkinnedMeshComponentExtensions.h"
#include "Components/SkinnedMeshComponent.h"

void UCSSkinnedMeshComponentExtensions::RefreshBoneTransforms(USkinnedMeshComponent* SkinnedMeshComponent)
{
	SkinnedMeshComponent->RefreshBoneTransforms();
}

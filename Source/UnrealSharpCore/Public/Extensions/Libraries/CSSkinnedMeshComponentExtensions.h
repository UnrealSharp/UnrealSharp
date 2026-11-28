#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "CSSkinnedMeshComponentExtensions.generated.h"

class USkinnedMeshComponent;

UCLASS(meta = (InternalType))
class UCSSkinnedMeshComponentExtensions : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()

public:

	/** Update the bone transforms, bone visibility and render data now, for a mesh that does not tick. */
	UFUNCTION(meta = (ExtensionMethod, ScriptMethod))
	static void RefreshBoneTransforms(USkinnedMeshComponent* SkinnedMeshComponent);
};

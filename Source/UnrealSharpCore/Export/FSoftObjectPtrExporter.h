#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "FSoftObjectPtrExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UFSoftObjectPtrExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void* LoadSynchronous(const TSoftObjectPtr<UObject>* SoftObjectPtr);
	
};

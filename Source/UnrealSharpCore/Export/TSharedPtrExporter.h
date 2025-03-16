#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "TSharedPtrExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UTSharedPtrExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void AddSharedReference(SharedPointerInternals::TReferenceControllerBase<ESPMode::ThreadSafe>* ReferenceController);

	UNREALSHARP_FUNCTION()
	static void ReleaseSharedReference(SharedPointerInternals::TReferenceControllerBase<ESPMode::ThreadSafe>* ReferenceController);
	
};

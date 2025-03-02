#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "CSTimerExtensions.generated.h"

using FNextTickEvent = void(*)();

UCLASS(meta = (NotGeneratorValid))
class UNREALSHARPCORE_API UCSTimerExtensions : public UFunctionsExporter
{
	GENERATED_BODY()
	
public:

	// UFunctions interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:
	static void SetTimerForNextTick(FNextTickEvent NextTickEvent);
};

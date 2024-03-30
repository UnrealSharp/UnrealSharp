#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "Async/Async.h"
#include "CSManagedGCHandle.h"
#include "AsyncExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class CSHARPFORUE_API UAsyncExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:
	
	// UFunctions interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:
	
	static void RunOnThread(ENamedThreads::Type Thread, GCHandleIntPtr DelegateHandle);
	static int GetCurrentNamedThread();
	
};

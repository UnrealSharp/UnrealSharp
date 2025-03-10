﻿#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "FScriptDelegateExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class UNREALSHARPCORE_API UFScriptDelegateExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:
	
	// UFunctionsExporter interface begin
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:

	static void BroadcastDelegate(FScriptDelegate* Delegate, void* Params);
	static bool IsBound(FScriptDelegate* Delegate);
	
};

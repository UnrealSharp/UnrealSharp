#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "FMsgExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UFMsgExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void Log(const UTF16CHAR* ManagedCategoryName, ELogVerbosity::Type Verbosity, const UTF16CHAR* ManagedMessage);
	
};

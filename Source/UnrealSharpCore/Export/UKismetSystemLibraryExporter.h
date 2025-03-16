#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "UKismetSystemLibraryExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UUKismetSystemLibraryExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void PrintString(const UObject* WorldContextObject, const UTF16CHAR* Message, float Duration, FLinearColor Color, bool PrintToScreen, bool PrintToConsole);
};

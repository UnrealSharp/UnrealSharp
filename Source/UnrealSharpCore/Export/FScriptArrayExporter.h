#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "FScriptArrayExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UFScriptArrayExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void* GetData(FScriptArray* Instance);

	UNREALSHARP_FUNCTION()
	static bool IsValidIndex(FScriptArray* Instance, int32 i);

	UNREALSHARP_FUNCTION()
	static int Num(FScriptArray* Instance);

	UNREALSHARP_FUNCTION()
	static void Destroy(FScriptArray* Instance);
	
};

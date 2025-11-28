#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "UStructExporter.generated.h"

UCLASS()
class UUStructExporter : public UObject
{
	GENERATED_BODY()
public:
	UNREALSHARP_FUNCTION()
	static void InitializeStruct(UStruct* Struct, void* Data);
};

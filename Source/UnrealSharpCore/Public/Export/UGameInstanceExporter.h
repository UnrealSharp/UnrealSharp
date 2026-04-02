#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "UGameInstanceExporter.generated.h"

UCLASS()
class UUGameInstanceExporter : public UObject
{
	GENERATED_BODY()

public:
	UNREALSHARP_FUNCTION()
	static void* GetGameInstanceSubsystem(UClass* SubsystemClass, UObject* WorldContextObject);
};

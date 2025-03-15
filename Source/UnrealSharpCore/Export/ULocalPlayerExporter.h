#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "ULocalPlayerExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UULocalPlayerExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void* GetLocalPlayerSubsystem(UClass* SubsystemClass, APlayerController* PlayerController);
};

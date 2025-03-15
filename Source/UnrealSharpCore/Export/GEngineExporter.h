#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "GEngineExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UGEngineExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void* GetEngineSubsystem(UClass* SubsystemClass);
	
};

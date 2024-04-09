// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "UWorldExporter.generated.h"

struct FSpawnActorParameters_Interop
{
	AActor* Owner;
	APawn* Instigator;
	AActor* Template;
	bool DeferConstruction;
	ESpawnActorCollisionHandlingMethod SpawnMethod;
};

UCLASS(meta = (NotGeneratorValid))
class CSHARPFORUE_API UUWorldExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:

	static void* SpawnActor(const UObject* Outer, const FTransform* SpawnTransform, UClass* Class, const FSpawnActorParameters_Interop* ManagedSpawnedParameters);
	static void SetTimer(UObject* Object, char* FunctionName, float Rate, bool Loop, FTimerHandle* TimerHandle);
	static void InvalidateTimer(UObject* Object, FTimerHandle* TimerHandle);
	static void* GetWorldSubsystem(UClass* SubsystemClass, UObject* WorldContextObject);
};

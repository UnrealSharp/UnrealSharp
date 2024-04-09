// Fill out your copyright notice in the Description page of Project Settings.


#include "UWorldExporter.h"

#include "CSharpForUE/CSManager.h"

void UUWorldExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(SpawnActor)
	EXPORT_FUNCTION(SetTimer)
	EXPORT_FUNCTION(InvalidateTimer)
	EXPORT_FUNCTION(GetWorldSubsystem)
}

void* UUWorldExporter::SpawnActor(const UObject* Outer, const FTransform* SpawnTransform, UClass* Class, const FSpawnActorParameters_Interop* ManagedSpawnedParameters)
{
	if (!IsValid(Outer) || !IsValid(Class))
	{
		return nullptr;
	}

	FActorSpawnParameters SpawnParameters;
	SpawnParameters.Instigator = ManagedSpawnedParameters->Instigator;
	SpawnParameters.Owner = ManagedSpawnedParameters->Owner;
	SpawnParameters.Template = ManagedSpawnedParameters->Template;
	SpawnParameters.bDeferConstruction = ManagedSpawnedParameters->DeferConstruction;
	SpawnParameters.SpawnCollisionHandlingOverride = ManagedSpawnedParameters->SpawnMethod;
	
	AActor* NewActor = Outer->GetWorld()->SpawnActor(Class, SpawnTransform, SpawnParameters);

	if (!IsValid(NewActor))
	{
		return nullptr;
	};

	return FCSManager::Get().FindManagedObject(NewActor).GetIntPtr();
}

void UUWorldExporter::SetTimer(UObject* Object, char* FunctionName, float Rate, bool Loop, FTimerHandle* TimerHandle)
{
	if (!IsValid(Object))
	{
		return;
	}

	FTimerDelegate TimerDelegate;
	TimerDelegate.BindUFunction(Object, FunctionName);
	
	Object->GetWorld()->GetTimerManager().SetTimer(*TimerHandle, TimerDelegate, Rate, Loop);
}

void UUWorldExporter::InvalidateTimer(UObject* Object, FTimerHandle* TimerHandle)
{
	if (!IsValid(Object))
	{
		return;
	}

	Object->GetWorld()->GetTimerManager().ClearTimer(*TimerHandle);
}

void* UUWorldExporter::GetWorldSubsystem(UClass* SubsystemClass, UObject* WorldContextObject)
{
	if (!IsValid(WorldContextObject))
	{
		return nullptr;
	}
	
	UWorldSubsystem* WorldSubsystem = WorldContextObject->GetWorld()->GetSubsystemBase(SubsystemClass);
	return FCSManager::Get().FindManagedObject(WorldSubsystem).GetIntPtr();
}

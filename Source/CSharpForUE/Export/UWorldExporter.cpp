#include "UWorldExporter.h"
#include "CSharpForUE/CSManager.h"
#include "Kismet/KismetSystemLibrary.h"

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

void UUWorldExporter::SetTimer(UObject* Object, FName FunctionName, float Rate, bool Loop, float InitialDelay, FTimerHandle* TimerHandle)
{
	FTimerDynamicDelegate Delegate;
	Delegate.BindUFunction(Object, FunctionName);
	*TimerHandle = UKismetSystemLibrary::K2_SetTimerDelegate(Delegate, Rate, Loop, false, InitialDelay);
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

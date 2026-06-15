#include "CSManager.h"
#include "Kismet/KismetSystemLibrary.h"

DECLARE_UNREALSHARP_EXPORTER(UWorldExporter)
{
	void SetTimer(UObject* Object, FName FunctionName, float Rate, bool Loop, float InitialDelay, FTimerHandle* TimerHandle)
	{
		FTimerDynamicDelegate Delegate;
		Delegate.BindUFunction(Object, FunctionName);
		*TimerHandle = UKismetSystemLibrary::K2_SetTimerDelegate(Delegate, Rate, Loop, false, InitialDelay);
	}

	void InvalidateTimer(UObject* Object, FTimerHandle* TimerHandle)
	{
		if (!IsValid(Object))
		{
			return;
		}

		Object->GetWorld()->GetTimerManager().ClearTimer(*TimerHandle);
	}

	void* GetWorldSubsystem(UClass* SubsystemClass, UObject* WorldContextObject)
	{
		if (!IsValid(WorldContextObject))
		{
			return nullptr;
		}
	
		UWorldSubsystem* WorldSubsystem = WorldContextObject->GetWorld()->GetSubsystemBase(SubsystemClass);
		return UCSManager::Get().FindManagedObject(WorldSubsystem);
	}

	void* GetNetMode(UObject* WorldContextObject)
	{
		if (!IsValid(WorldContextObject))
		{
			return nullptr;
		}
		return (void*)WorldContextObject->GetWorld()->GetNetMode();
	}
	
	EXPORT_UNREALSHARP_FUNCTION(SetTimer)
	EXPORT_UNREALSHARP_FUNCTION(InvalidateTimer)
	EXPORT_UNREALSHARP_FUNCTION(GetWorldSubsystem)
	EXPORT_UNREALSHARP_FUNCTION(GetNetMode)
}

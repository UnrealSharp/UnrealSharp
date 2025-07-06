#include "CSClass.h"
#include "UnrealSharpCore.h"

#if WITH_EDITOR
void UCSClass::PostDuplicate(bool bDuplicateForPIE)
{
	Super::PostDuplicate(bDuplicateForPIE);
	
	UBlueprint* Blueprint = Cast<UBlueprint>(ClassGeneratedBy);
	if (!IsValid(Blueprint))
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("PostDuplicate called on a class without a valid Blueprint: %s"), *GetName());
		return;
	}
	
	UCSClass* ManagedClass = Cast<UCSClass>(Blueprint->GeneratedClass);
	if (!IsValid(ManagedClass))
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("PostDuplicate called on a class that is not a UCSClass: %s"), *GetName());
	}
	
	TypeInfo = ManagedClass->GetTypeInfo();
}
#endif

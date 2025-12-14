#include "Types/CSClass.h"

#include "CSManagedAssembly.h"
#include "UnrealSharpCore.h"
#include "Utilities/CSClassUtilities.h"

void UCSClass::ManagedObjectConstructor(const FObjectInitializer& ObjectInitializer)
{
	UObject* Object = ObjectInitializer.GetObj();
	
	UCSClass* FirstManagedClass = FCSClassUtilities::GetFirstManagedClass(Object->GetClass());
	UClass* FirstNativeClass = FCSClassUtilities::GetFirstNativeClass(FirstManagedClass);
	
	// Execute the native class' constructor first.
	FirstNativeClass->ClassConstructor(ObjectInitializer);

	// Initialize managed properties that are not zero initialized such as FText.
	for (TFieldIterator<FProperty> PropertyIt(FirstManagedClass); PropertyIt; ++PropertyIt)
	{
		FProperty* Property = *PropertyIt;

		if (!FCSClassUtilities::IsManagedClass(Property->GetOwnerClass()))
		{
			// We don't want to initialize properties that are not from a managed class
			break;
		}
		
		if (Property->HasAnyPropertyFlags(CPF_ZeroConstructor))
		{
			continue;
		}

		Property->InitializeValue_InContainer(Object);
	}

#if WITH_EDITOR
	if (FirstManagedClass->IsCreationDeferred())
	{
		return;
	}
#endif
	
	TSharedPtr<FCSManagedTypeDefinition> ManagedTypeDefinition = FirstManagedClass->GetManagedTypeDefinition();
	UCSManagedAssembly* OwningAssembly = ManagedTypeDefinition->GetOwningAssembly();
	
	OwningAssembly->CreateManagedObjectFromNative(Object, ManagedTypeDefinition->GetTypeGCHandle());
}

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
	
	SetManagedTypeDefinition(ManagedClass->GetManagedTypeDefinition());
}

void UCSClass::PurgeClass(bool bRecompilingOnLoad)
{
	Super::PurgeClass(bRecompilingOnLoad);
	NumReplicatedProperties = 0;
}
#endif

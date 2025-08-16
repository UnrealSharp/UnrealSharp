#include "CSBuilderManager.h"
#include "CSGeneratedTypeBuilder.h"
#include "UnrealSharpCore.h"
#include "TypeInfo/CSManagedTypeInfo.h"
#include "UnrealSharpUtilities/UnrealSharpUtils.h"

void UCSTypeBuilderManager::Initialize()
{
	if (TypeBuilders.Num() > 0)
	{
		UE_LOG(LogUnrealSharp, Warning, TEXT("UCSTypeBuilderManager is already initialized."));
		return;
	}
	
	TArray<UCSGeneratedTypeBuilder*> AllBuilders;
	FCSUnrealSharpUtils::GetAllCDOsOfClass<UCSGeneratedTypeBuilder>(AllBuilders);

	TypeBuilders.Reserve(AllBuilders.Num());

	for (UCSGeneratedTypeBuilder* Builder : AllBuilders)
	{
		if (!IsValid(Builder))
		{
			UE_LOG(LogUnrealSharp, Warning, TEXT("Invalid type builder found: %s"), *Builder->GetName());
			continue;
		}

		UCSGeneratedTypeBuilder* NewBuilder = NewObject<UCSGeneratedTypeBuilder>(this, Builder->GetClass(), NAME_None, RF_Transient | RF_Public);
		uint32 TypeClassID = Builder->GetFieldType()->GetUniqueID();
		TypeBuilders.AddByHash(TypeClassID, TypeClassID, NewBuilder);
	}
}

const UCSGeneratedTypeBuilder* UCSTypeBuilderManager::BorrowTypeBuilder(const TSharedPtr<FCSManagedTypeInfo>& ManagedTypeInfo)
{
	UClass* TypeClass = ManagedTypeInfo->GetFieldClass();

	if (!IsValid(TypeClass))
	{
		UE_LOG(LogUnrealSharp, Warning, TEXT("Invalid type class for managed type info"));
		return nullptr;
	}

	uint32 TypeClassID = TypeClass->GetUniqueID();
	if (TObjectPtr<UCSGeneratedTypeBuilder>* ExistingBuilder = TypeBuilders.FindByHash(TypeClassID, TypeClassID))
	{
		return *ExistingBuilder;
	}

	UE_LOG(LogUnrealSharp, Warning, TEXT("No type builder found for class: %s"), *TypeClass->GetName());
	return nullptr;
}

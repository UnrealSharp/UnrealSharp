#include "CSGeneratedEnumBuilder.h"
#include "CSManager.h"
#include "MetaData/CSEnumMetaData.h"
#include "TypeGenerator/CSEnum.h"
#include "UnrealSharpUtilities/UnrealSharpUtils.h"

DEFINE_BUILDER_TYPE(UCSGeneratedEnumBuilder, UCSEnum, FCSEnumMetaData)

void UCSGeneratedEnumBuilder::RebuildType()
{
	PurgeEnum();
	
	const int32 NumItems = TypeMetaData->Items.Num();
    
	TArray<TPair<FName, int64>> Entries;
	Entries.Reserve(NumItems);

	const FString EnumName = Field->GetName();
	for (int32 i = 0; i < NumItems; i++)
	{
		FString ItemName = FString::Printf(TEXT("%s::%s"), *EnumName, *TypeMetaData->Items[i].ToString());
		Entries.Emplace(ItemName, i);
		Field->DisplayNameMap.Add(*ItemName, FText::FromString(ItemName));
	}
	
	Field->SetEnums(Entries, UEnum::ECppForm::EnumClass);
	RegisterFieldToLoader(ENotifyRegistrationType::NRT_Enum);

#if WITH_EDITOR
	UCSManager::Get().OnNewEnumEvent().Broadcast(Field);
#endif
}

UClass* UCSGeneratedEnumBuilder::GetFieldType() const
{
	return UCSEnum::StaticClass();
}

#if WITH_EDITOR
void UCSGeneratedEnumBuilder::UpdateType()
{
	UCSManager::Get().OnEnumReloadedEvent().Broadcast(Field);
}
#endif

void UCSGeneratedEnumBuilder::PurgeEnum() const
{
	Field->DisplayNameMap.Empty();
	FCSUnrealSharpUtils::PurgeMetaData(Field);
}

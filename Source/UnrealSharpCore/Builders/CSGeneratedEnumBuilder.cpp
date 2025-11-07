#include "CSGeneratedEnumBuilder.h"
#include "CSManager.h"
#include "MetaData/CSEnumMetaData.h"
#include "Types/CSEnum.h"
#include "UnrealSharpUtilities/UnrealSharpUtils.h"

UCSGeneratedEnumBuilder::UCSGeneratedEnumBuilder()
{
	FieldType = UCSEnum::StaticClass();
}

void UCSGeneratedEnumBuilder::RebuildType(UField* TypeToBuild, const TSharedPtr<FCSManagedTypeInfo>& ManagedTypeInfo) const
{
	UCSEnum* Field = static_cast<UCSEnum*>(TypeToBuild);
	TSharedPtr<FCSEnumMetaData> TypeMetaData = ManagedTypeInfo->GetTypeMetaData<FCSEnumMetaData>();
	
	PurgeEnum(Field);
	
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
	RegisterFieldToLoader(TypeToBuild, ENotifyRegistrationType::NRT_Enum);

#if WITH_EDITOR
	UCSManager::Get().OnNewEnumEvent().Broadcast(Field);
#endif
}

void UCSGeneratedEnumBuilder::PurgeEnum(UCSEnum* Field)
{
	Field->DisplayNameMap.Reset();
	FCSUnrealSharpUtils::PurgeMetaData(Field);
}

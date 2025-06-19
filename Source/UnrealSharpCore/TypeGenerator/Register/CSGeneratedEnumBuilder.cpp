#include "CSGeneratedEnumBuilder.h"

#include "CSManager.h"

void FCSGeneratedEnumBuilder::RebuildType()
{
	PurgeEnum();

	if (!Field->HasTypeInfo())
	{
		TSharedPtr<FCSEnumInfo> EnumInfo = OwningAssembly->FindEnumInfo(TypeMetaData->FieldName);
		Field->SetTypeInfo(EnumInfo);
	}
	
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
	
	Field->SetEnums(Entries, UEnum::ECppForm::Namespaced);
	RegisterFieldToLoader(ENotifyRegistrationType::NRT_Enum);

#if WITH_EDITOR
	UCSManager::Get().OnNewEnumEvent().Broadcast(Field);
#endif
}

#if WITH_EDITOR
void FCSGeneratedEnumBuilder::UpdateType()
{
	UCSManager::Get().OnEnumReloadedEvent().Broadcast(Field);
}
#endif

void FCSGeneratedEnumBuilder::PurgeEnum() const
{
	Field->DisplayNameMap.Empty();
}

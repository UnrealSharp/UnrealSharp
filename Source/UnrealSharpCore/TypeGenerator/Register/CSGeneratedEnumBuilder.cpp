#include "CSGeneratedEnumBuilder.h"

void FCSGeneratedEnumBuilder::StartBuildingType()
{
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
}
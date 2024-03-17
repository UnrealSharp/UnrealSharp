#include "CSGeneratedEnumBuilder.h"
#include "CSMetaData.h"

void FCSGeneratedEnumBuilder::StartBuildingType()
{
	const int32 NumItems = TypeMetaData->Items.Num();
    
	TArray<TPair<FName, int64>> Entries;
	Entries.Reserve(NumItems);

	for (int32 i = 0; i < NumItems; i++)
	{
		Entries.Emplace(TypeMetaData->Items[i], i);
	}

	Field->SetEnums(Entries, UEnum::ECppForm::Regular);
	RegisterFieldToLoader(ENotifyRegistrationType::NRT_Enum);
}

void FCSGeneratedEnumBuilder::NewField(UEnum* OldField, UEnum* NewField)
{
	FCSTypeRegistry::Get().GetOnNewEnumEvent().Broadcast(OldField, NewField);
}

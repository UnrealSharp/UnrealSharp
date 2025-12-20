#include "Compilers/CSManagedEnumCompiler.h"
#include "CSManager.h"
#include "ReflectionData/CSEnumReflectionData.h"
#include "Types/CSEnum.h"
#include "UnrealSharpUtils.h"

#if WITH_EDITOR
#include "Kismet2/EnumEditorUtils.h"
#endif

UCSManagedEnumCompiler::UCSManagedEnumCompiler()
{
	FieldType = UCSEnum::StaticClass();
}

void UCSManagedEnumCompiler::Recompile(UField* TypeToRecompile, const TSharedPtr<FCSManagedTypeDefinition>& ManagedTypeDefinition) const
{
	UCSEnum* Enum = static_cast<UCSEnum*>(TypeToRecompile);
	TSharedPtr<FCSEnumReflectionData> EnumReflectionData = ManagedTypeDefinition->GetReflectionData<FCSEnumReflectionData>();
	
	PurgeEnum(Enum);

	const int32 NumItems = EnumReflectionData->EnumNames.Num();
    
	TArray<TPair<FName, int64>> Entries;
	Entries.Reserve(NumItems);

	const FString EnumName = Enum->GetName();
	for (int32 i = 0; i < NumItems; i++)
	{
		FString ItemName = FString::Printf(TEXT("%s::%s"), *EnumName, *EnumReflectionData->EnumNames[i]);
		Entries.Emplace(ItemName, i);
		Enum->DisplayNameMap.Add(*ItemName, FText::FromString(ItemName));
	}
	
	Enum->SetEnums(Entries, UEnum::ECppForm::EnumClass);
	RegisterFieldToLoader(TypeToRecompile, ENotifyRegistrationType::NRT_Enum);

#if WITH_EDITOR
	UCSManager::Get().OnNewEnumEvent().Broadcast(Enum);
	FEnumEditorUtils::SetEnumeratorBitflagsTypeState(Enum, false);
#endif
}

void UCSManagedEnumCompiler::PurgeEnum(UCSEnum* Field)
{
	Field->DisplayNameMap.Reset();
	FCSUnrealSharpUtils::PurgeMetaData(Field);
}

#include "Builders/CSGeneratedStructBuilder.h"
#include "CSManager.h"
#include "MetaData/CSStructMetaData.h"
#include "Types/CSScriptStruct.h"
#include "Factories/CSPropertyFactory.h"
#include "UnrealSharpUtils.h"

#if WITH_EDITOR
#include "UserDefinedStructure/UserDefinedStructEditorData.h"
#endif

UCSGeneratedStructBuilder::UCSGeneratedStructBuilder()
{
	FieldType = UCSScriptStruct::StaticClass();
}

void UCSGeneratedStructBuilder::RebuildType(UField* TypeToBuild, const TSharedPtr<FCSManagedTypeInfo>& ManagedTypeInfo) const
{
	UCSScriptStruct* Field = static_cast<UCSScriptStruct*>(TypeToBuild);
	TSharedPtr<FCSStructMetaData> TypeMetaData = ManagedTypeInfo->GetTypeMetaData<FCSStructMetaData>();
	
	PurgeStruct(Field);
	
	FCSPropertyFactory::CreateAndAssignProperties(Field, TypeMetaData->Properties);
    
	Field->Status = UDSS_UpToDate;
	if (!Field->Guid.IsValid())
	{
		Field->Guid = FCSUnrealSharpUtils::ConstructGUIDFromString(Field->GetName());
	}
	
	Field->Bind();
	Field->StaticLink(true);
	Field->RecreateDefaults();
	Field->UpdateStructFlags();
	
	RegisterFieldToLoader(TypeToBuild, ENotifyRegistrationType::NRT_Struct);

#if WITH_EDITOR
	UCSManager::Get().OnNewStructEvent().Broadcast(Field);
#endif
}

void UCSGeneratedStructBuilder::PurgeStruct(UCSScriptStruct* Field)
{
	FCSUnrealSharpUtils::PurgeStruct(Field);
#if WITH_EDITORONLY_DATA
	Field->PrimaryStruct = nullptr;
	Field->EditorData = nullptr;
#endif
}

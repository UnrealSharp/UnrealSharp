#include "CSGeneratedStructBuilder.h"

#include "CSManager.h"
#include "MetaData/CSStructMetaData.h"
#include "UnrealSharpCore/TypeGenerator/CSScriptStruct.h"
#include "UnrealSharpCore/TypeGenerator/Factories/CSPropertyFactory.h"
#include "UnrealSharpUtilities/UnrealSharpUtils.h"

#if WITH_EDITOR
#include "UserDefinedStructure/UserDefinedStructEditorData.h"
#endif

DEFINE_BUILDER_TYPE(UCSGeneratedStructBuilder, UCSScriptStruct, FCSStructMetaData)

void UCSGeneratedStructBuilder::RebuildType()
{
	PurgeStruct();

	if (!Field->HasTypeInfo())
	{
		TSharedPtr<FCSManagedTypeInfo> StructInfo = GetOwningAssembly()->FindStructInfo(TypeMetaData->FieldName);
		Field->SetTypeInfo(StructInfo);
	}
	
	FCSPropertyFactory::CreateAndAssignProperties(Field, TypeMetaData->Properties);
    FCSMetaDataUtils::ApplyMetaData(TypeMetaData->MetaData, Field);
    
	Field->Status = UDSS_UpToDate;
	if (!Field->Guid.IsValid())
	{
		Field->Guid = FCSUnrealSharpUtils::ConstructGUIDFromString(Field->GetName());
	}
	
	Field->Bind();
	Field->StaticLink(true);
	Field->RecreateDefaults();
	Field->UpdateStructFlags();
	
	RegisterFieldToLoader(ENotifyRegistrationType::NRT_Struct);

#if WITH_EDITOR
	UCSManager::Get().OnNewStructEvent().Broadcast(Field);
#endif
}

UClass* UCSGeneratedStructBuilder::GetFieldType() const
{
	return UCSScriptStruct::StaticClass();
}

#if WITH_EDITOR
void UCSGeneratedStructBuilder::UpdateType()
{
	UCSManager::Get().OnStructReloadedEvent().Broadcast(Field);
}
#endif

void UCSGeneratedStructBuilder::PurgeStruct()
{
	FCSUnrealSharpUtils::PurgeStruct(Field);
#if WITH_EDITORONLY_DATA
	Field->PrimaryStruct = nullptr;
	Field->EditorData = nullptr;
#endif
}

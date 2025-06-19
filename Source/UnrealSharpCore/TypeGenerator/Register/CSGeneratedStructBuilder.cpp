#include "CSGeneratedStructBuilder.h"

#include "CSManager.h"
#include "UnrealSharpCore/TypeGenerator/CSScriptStruct.h"
#include "UnrealSharpCore/TypeGenerator/Factories/CSPropertyFactory.h"
#include "UnrealSharpUtilities/UnrealSharpUtils.h"

#if WITH_EDITOR
#include "UserDefinedStructure/UserDefinedStructEditorData.h"
#endif

void FCSGeneratedStructBuilder::RebuildType()
{
	PurgeStruct();

	if (!Field->HasTypeInfo())
	{
		TSharedPtr<FCSStructInfo> StructInfo = OwningAssembly->FindStructInfo(TypeMetaData->FieldName);
		Field->SetTypeInfo(StructInfo);
	}
	
	FCSPropertyFactory::CreateAndAssignProperties(Field, TypeMetaData->Properties);
		
	Field->Status = UDSS_UpToDate;
	if (!Field->Guid.IsValid())
	{
		Field->Guid = FGuid::NewGuid();
	}
	
	Field->Bind();
	Field->StaticLink(true);
	Field->RecreateDefaults();
	
	RegisterFieldToLoader(ENotifyRegistrationType::NRT_Struct);

#if WITH_EDITOR
	UCSManager::Get().OnNewStructEvent().Broadcast(Field);
#endif
}

#if WITH_EDITOR
void FCSGeneratedStructBuilder::UpdateType()
{
	UCSManager::Get().OnStructReloadedEvent().Broadcast(Field);
}
#endif

void FCSGeneratedStructBuilder::PurgeStruct()
{
	FUnrealSharpUtils::PurgeStruct(Field);
#if WITH_EDITORONLY_DATA
	Field->PrimaryStruct = nullptr;
	Field->EditorData = nullptr;
#endif
}

#include "CSGeneratedStructBuilder.h"
#include "CSTypeRegistry.h"
#include "UnrealSharpCore/TypeGenerator/CSScriptStruct.h"
#include "UnrealSharpCore/TypeGenerator/Factories/CSPropertyFactory.h"

#if WITH_EDITOR
#include "UserDefinedStructure/UserDefinedStructEditorData.h"
#endif

void FCSGeneratedStructBuilder::StartBuildingType()
{
	PurgeStruct();
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
	FCSTypeRegistry::Get().GetOnNewStructEvent().Broadcast(Field);
#endif
}

void FCSGeneratedStructBuilder::PurgeStruct()
{
	Field->PropertyLink = nullptr;
	Field->DestructorLink = nullptr;
	Field->ChildProperties = nullptr;
	Field->Children = nullptr;
	Field->PropertiesSize = 0;
	Field->MinAlignment = 0;
	Field->RefLink = nullptr;
#if WITH_EDITOR
#if ENGINE_MINOR_VERSION >= 5
	Field->TotalFieldCount = 0;
#endif
	Field->PrimaryStruct = nullptr;
	Field->EditorData = nullptr;
#endif
}

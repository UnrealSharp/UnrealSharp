#include "CSGeneratedStructBuilder.h"
#include "CSMetaData.h"
#include "CSharpForUE/TypeGenerator/CSScriptStruct.h"
#include "CSharpForUE/TypeGenerator/Factories/CSPropertyFactory.h"

#if WITH_EDITOR
#include "UserDefinedStructure/UserDefinedStructEditorData.h"
#endif

void FCSGeneratedStructBuilder::StartBuildingType()
{
	FCSPropertyFactory::GeneratePropertiesForType(Field, TypeMetaData->Properties);
	
#if WITH_EDITOR
	Field->EditorData = NewObject<UUserDefinedStructEditorData>(Field, NAME_None, RF_Transactional);
	Field->SetMetaData(TEXT("BlueprintType"), TEXT("true"));
#endif
		
	Field->Status = UDSS_UpToDate;
	Field->Guid = FGuid::NewGuid();
	
	Field->Bind();
	Field->StaticLink(true);
	Field->RecreateDefaults();
	
	RegisterFieldToLoader(ENotifyRegistrationType::NRT_Struct);
}

void FCSGeneratedStructBuilder::NewField(UCSScriptStruct* OldField, UCSScriptStruct* NewField)
{
	FCSTypeRegistry::Get().GetOnNewStructEvent().Broadcast(OldField, NewField);
}

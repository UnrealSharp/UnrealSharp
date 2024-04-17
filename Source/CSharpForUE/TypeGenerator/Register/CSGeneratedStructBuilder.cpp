#include "CSGeneratedStructBuilder.h"
#include "CSMetaData.h"
#include "CSharpForUE/TypeGenerator/CSScriptStruct.h"
#include "CSharpForUE/TypeGenerator/Factories/CSPropertyFactory.h"
#include "UserDefinedStructure/UserDefinedStructEditorData.h"

void FCSGeneratedStructBuilder::StartBuildingType()
{
	FCSPropertyFactory::GeneratePropertiesForType(Field, TypeMetaData->Properties);
	Field->EditorData = NewObject<UUserDefinedStructEditorData>(Field, NAME_None, RF_Transactional);
	Field->Guid = FGuid::NewGuid();
	Field->SetMetaData(TEXT("BlueprintType"), TEXT("true"));
	Field->Bind();
	Field->StaticLink(true);
	Field->Status = UDSS_UpToDate;
	Field->RecreateDefaults();
	RegisterFieldToLoader(ENotifyRegistrationType::NRT_Struct);
}

void FCSGeneratedStructBuilder::NewField(UCSScriptStruct* OldField, UCSScriptStruct* NewField)
{
	FCSTypeRegistry::Get().GetOnNewStructEvent().Broadcast(OldField, NewField);
}

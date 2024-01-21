#include "CSGeneratedStructBuilder.h"
#include "CSMetaData.h"
#include "CSharpForUE/TypeGenerator/CSScriptStruct.h"
#include "CSharpForUE/TypeGenerator/Factories/CSPropertyFactory.h"

void FCSGeneratedStructBuilder::StartBuildingType()
{
	FCSPropertyFactory::GeneratePropertiesForType(Field, TypeMetaData->Properties);
	Field->StaticLink(true);
	Field->RecreateDefaults();
}

void FCSGeneratedStructBuilder::NewField(UCSScriptStruct* OldField, UCSScriptStruct* NewField)
{
	FCSTypeRegistry::Get().GetOnNewStructEvent().Broadcast(OldField, NewField);
}

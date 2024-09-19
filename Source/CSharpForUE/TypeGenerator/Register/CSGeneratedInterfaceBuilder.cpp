#include "CSGeneratedInterfaceBuilder.h"
#include "CSTypeRegistry.h"
#include "CSharpForUE/TypeGenerator/Factories/CSFunctionFactory.h"

void FCSGeneratedInterfaceBuilder::StartBuildingType()
{
	Field->SetSuperStruct(UInterface::StaticClass());
	Field->ClassFlags |= CLASS_Interface;
	
	FCSFunctionFactory::GenerateFunctions(Field, TypeMetaData->Functions);

	Field->ClassConstructor = UInterface::StaticClass()->ClassConstructor;
	
	Field->StaticLink(true);
	Field->Bind();
	Field->AssembleReferenceTokenStream();
	Field->GetDefaultObject();
}

void FCSGeneratedInterfaceBuilder::NewField(UClass* OldField, UClass* NewField)
{
	FCSTypeRegistry::Get().GetOnNewClassEvent().Broadcast(OldField, NewField);
}

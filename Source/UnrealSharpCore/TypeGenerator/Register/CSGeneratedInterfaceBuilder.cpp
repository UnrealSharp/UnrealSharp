#include "CSGeneratedInterfaceBuilder.h"
#include "CSTypeRegistry.h"
#include "UnrealSharpCore/TypeGenerator/Factories/CSFunctionFactory.h"

void FCSGeneratedInterfaceBuilder::StartBuildingType()
{
	Field->PurgeClass(true);
	Field->SetSuperStruct(UInterface::StaticClass());
	Field->ClassFlags |= CLASS_Interface;
	
	FCSFunctionFactory::GenerateFunctions(Field, TypeMetaData->Functions);

	Field->ClassConstructor = UInterface::StaticClass()->ClassConstructor;
	
	Field->StaticLink(true);
	Field->Bind();
	Field->AssembleReferenceTokenStream();
	Field->GetDefaultObject();

#if WITH_EDITOR
	FCSTypeRegistry::Get().GetOnNewInterfaceEvent().Broadcast(Field);
#endif
}

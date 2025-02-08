#include "CSGeneratedInterfaceBuilder.h"
#include "UnrealSharpCore/TypeGenerator/Factories/CSFunctionFactory.h"

void FCSGeneratedInterfaceBuilder::RebuildType()
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
	UCSManager::Get().OnNewInterfaceEvent().Broadcast(Field);
#endif
}

void FCSGeneratedInterfaceBuilder::UpdateType()
{
	UCSManager::Get().OnInterfaceReloadedEvent().Broadcast(Field);
}

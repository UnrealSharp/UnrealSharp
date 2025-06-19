#include "CSGeneratedInterfaceBuilder.h"

#include "CSManager.h"
#include "UnrealSharpCore/TypeGenerator/Factories/CSFunctionFactory.h"

void FCSGeneratedInterfaceBuilder::RebuildType()
{
	Field->PurgeClass(true);

	if (!Field->HasTypeInfo())
	{
		TSharedPtr<FCSInterfaceInfo> InterfaceInfo = OwningAssembly->FindInterfaceInfo(TypeMetaData->FieldName);
		Field->SetTypeInfo(InterfaceInfo);
	}
	
	Field->SetSuperStruct(UInterface::StaticClass());
	Field->ClassFlags |= CLASS_Interface;
	
	FCSFunctionFactory::GenerateFunctions(Field, TypeMetaData->Functions);
	RegisterFunctionsToLoader();

	Field->ClassConstructor = UInterface::StaticClass()->ClassConstructor;
	
	Field->StaticLink(true);
	Field->Bind();
	Field->AssembleReferenceTokenStream();
	Field->GetDefaultObject();

#if WITH_EDITOR
	UCSManager::Get().OnNewInterfaceEvent().Broadcast(Field);
#endif
}

#if WITH_EDITOR
void FCSGeneratedInterfaceBuilder::UpdateType()
{
	UCSManager::Get().OnInterfaceReloadedEvent().Broadcast(Field);
}
#endif

void FCSGeneratedInterfaceBuilder::RegisterFunctionsToLoader()
{
	for (TFieldIterator<UFunction> It(Field, EFieldIterationFlags::None); It; ++It)
	{
		UFunction* Function = *It;
		
		NotifyRegistrationEvent(*Function->GetOutermost()->GetName(),
		*Function->GetName(),
		ENotifyRegistrationType::NRT_Struct,
		ENotifyRegistrationPhase::NRP_Finished,
		nullptr,
		false,
		Function);
	}
}

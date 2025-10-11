#include "CSGeneratedInterfaceBuilder.h"
#include "CSManager.h"
#include "TypeGenerator/CSInterface.h"
#include "UnrealSharpCore/TypeGenerator/Factories/CSFunctionFactory.h"

void UCSGeneratedInterfaceBuilder::RebuildType(UField* TypeToBuild, const TSharedPtr<FCSManagedTypeInfo>& ManagedTypeInfo) const
{
	UCSInterface* Field = static_cast<UCSInterface*>(TypeToBuild);
	TSharedPtr<FCSClassBaseMetaData> TypeMetaData = ManagedTypeInfo->GetTypeMetaData<FCSClassBaseMetaData>();
	
	Field->PurgeClass(true);

	UClass* ParentInterface;
	if (TypeMetaData->ParentClass.IsValid())
	{
		ParentInterface = TypeMetaData->ParentClass.GetAsInterface();
	}
	else
	{
		ParentInterface = UInterface::StaticClass();
	}
	
	Field->SetSuperStruct(ParentInterface);
	Field->ClassFlags |= CLASS_Interface;
	
	FCSFunctionFactory::GenerateFunctions(Field, TypeMetaData->Functions);
	RegisterFunctionsToLoader(Field);

	Field->ClassConstructor = UInterface::StaticClass()->ClassConstructor;
	
	Field->StaticLink(true);
	Field->Bind();
	Field->AssembleReferenceTokenStream();
	Field->GetDefaultObject();

#if WITH_EDITOR
	UCSManager::Get().OnNewInterfaceEvent().Broadcast(Field);
#endif
}

UClass* UCSGeneratedInterfaceBuilder::GetFieldType() const
{
	return UCSInterface::StaticClass();
}

void UCSGeneratedInterfaceBuilder::RegisterFunctionsToLoader(UCSInterface* Field)
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

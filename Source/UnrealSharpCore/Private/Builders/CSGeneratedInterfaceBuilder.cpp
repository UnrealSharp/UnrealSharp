#include "Builders/CSGeneratedInterfaceBuilder.h"
#include "CSManager.h"
#include "Types/CSInterface.h"
#include "Factories/CSFunctionFactory.h"

UCSGeneratedInterfaceBuilder::UCSGeneratedInterfaceBuilder()
{
	FieldType = UCSInterface::StaticClass();
}

void UCSGeneratedInterfaceBuilder::RebuildType(UField* TypeToBuild, const TSharedPtr<FCSManagedTypeInfo>& ManagedTypeInfo) const
{
	UCSInterface* Field = static_cast<UCSInterface*>(TypeToBuild);
	TSharedPtr<FCSClassBaseMetaData> TypeMetaData = ManagedTypeInfo->GetMetaData<FCSClassBaseMetaData>();
	
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

	Field->ClassConstructor = UInterface::StaticClass()->ClassConstructor;
	
	Field->StaticLink(true);
	Field->Bind();
	Field->AssembleReferenceTokenStream();
	Field->GetDefaultObject();

#if WITH_EDITOR
	UCSManager::Get().OnNewInterfaceEvent().Broadcast(Field);
#endif

	RegisterFieldToLoader(TypeToBuild, ENotifyRegistrationType::NRT_Class);
}

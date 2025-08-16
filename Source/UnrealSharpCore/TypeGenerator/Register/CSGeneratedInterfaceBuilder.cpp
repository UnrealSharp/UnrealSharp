#include "CSGeneratedInterfaceBuilder.h"
#include "CSManager.h"
#include "CSMetaDataUtils.h"
#include "MetaData/CSInterfaceMetaData.h"
#include "TypeGenerator/CSInterface.h"
#include "UnrealSharpCore/TypeGenerator/Factories/CSFunctionFactory.h"

void UCSGeneratedInterfaceBuilder::RebuildType(UField* TypeToBuild, const TSharedPtr<FCSManagedTypeInfo>& ManagedTypeInfo) const
{
	UCSInterface* Field = CastChecked<UCSInterface>(TypeToBuild);
	TSharedPtr<FCSInterfaceMetaData> TypeMetaData = ManagedTypeInfo->GetTypeMetaData<FCSInterfaceMetaData>();
	
	Field->PurgeClass(true);

	UClass* ParentInterface = UInterface::StaticClass();
	if (TypeMetaData->ParentInterface.IsValid())
	{
		ParentInterface = TypeMetaData->ParentInterface.GetOwningInterface();
	}
	
	Field->SetSuperStruct(ParentInterface);
	
	Field->ClassFlags |= CLASS_Interface;
    FCSMetaDataUtils::ApplyMetaData(TypeMetaData->MetaData, Field);
	
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

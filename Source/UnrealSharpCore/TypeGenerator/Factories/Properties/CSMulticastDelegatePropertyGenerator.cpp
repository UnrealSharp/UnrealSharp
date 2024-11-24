#include "CSMulticastDelegatePropertyGenerator.h"
#include "TypeGenerator/Factories/CSFunctionFactory.h"
#include "TypeGenerator/Register/MetaData/CSDelegateMetaData.h"

FProperty* UCSMulticastDelegatePropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FMulticastInlineDelegateProperty* NewProperty =
		static_cast<FMulticastInlineDelegateProperty*>(Super::CreateProperty(Outer, PropertyMetaData));
	TSharedPtr<FCSDelegateMetaData> MulticastDelegateMetaData = PropertyMetaData.GetTypeMetaData<FCSDelegateMetaData>();
	UClass* Class = CastChecked<UClass>(Outer);
	
	UFunction* SignatureFunction = FCSFunctionFactory::CreateFunctionFromMetaData(Class, MulticastDelegateMetaData->SignatureFunction);
	NewProperty->SignatureFunction = SignatureFunction;
	return NewProperty;
}

void UCSMulticastDelegatePropertyGenerator::CreatePinInfoEditor(const FCSPropertyMetaData& PropertyMetaData,
	FEdGraphPinType& PinType)
{
	PinType.PinCategory = UEdGraphSchema_K2::PC_MCDelegate;
}

UObject* UCSMulticastDelegatePropertyGenerator::GetPinSubCategoryObject(UBlueprint* Blueprint, const FCSPropertyMetaData& PropertyMetaData) const
{
	UClass* Class = CastChecked<UClass>(Blueprint->GeneratedClass);
	TSharedPtr<FCSDelegateMetaData> MulticastDelegateMetaData = PropertyMetaData.GetTypeMetaData<FCSDelegateMetaData>();
	return FCSFunctionFactory::CreateFunctionFromMetaData(Class, MulticastDelegateMetaData->SignatureFunction);
}

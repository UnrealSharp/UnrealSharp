#include "CSDelegatePropertyGenerator.h"
#include "TypeGenerator/Factories/CSFunctionFactory.h"
#include "TypeGenerator/Register/MetaData/CSDelegateMetaData.h"

FProperty* UCSDelegatePropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FDelegateProperty* NewProperty = static_cast<FDelegateProperty*>(Super::CreateProperty(Outer, PropertyMetaData));
	TSharedPtr<FCSDelegateMetaData> DelegateMetaData = PropertyMetaData.GetTypeMetaData<FCSDelegateMetaData>();
	NewProperty->SignatureFunction = FCSFunctionFactory::CreateFunctionFromMetaData(Outer->GetOwnerClass(), DelegateMetaData->SignatureFunction);
	return NewProperty;
}

void UCSDelegatePropertyGenerator::CreatePinInfoEditor(const FCSPropertyMetaData& PropertyMetaData,
	FEdGraphPinType& PinType)
{
	PinType.PinCategory = UEdGraphSchema_K2::PC_Delegate;
}

UObject* UCSDelegatePropertyGenerator::GetPinSubCategoryObject(UBlueprint* Blueprint,
	const FCSPropertyMetaData& PropertyMetaData) const
{
	UClass* Class = CastChecked<UClass>(Blueprint->GeneratedClass);
	TSharedPtr<FCSDelegateMetaData> MulticastDelegateMetaData = PropertyMetaData.GetTypeMetaData<FCSDelegateMetaData>();
	return FCSFunctionFactory::CreateFunctionFromMetaData(Class, MulticastDelegateMetaData->SignatureFunction);
}

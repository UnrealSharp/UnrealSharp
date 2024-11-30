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
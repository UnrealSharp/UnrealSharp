#include "CSMulticastDelegatePropertyGenerator.h"

#include "TypeGenerator/Factories/CSFunctionFactory.h"
#include "TypeGenerator/Factories/CSPropertyFactory.h"
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

TSharedPtr<FCSUnrealType> UCSMulticastDelegatePropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSDelegateMetaData>();
}

#if WITH_EDITOR
FEdGraphPinType UCSMulticastDelegatePropertyGenerator::GetPinType(ECSPropertyType PropertyType, const FCSPropertyMetaData& MetaData, UBlueprint* Outer) const
{
	return MakeDelegate(UEdGraphSchema_K2::PC_MCDelegate, MetaData, Outer);
}
#endif

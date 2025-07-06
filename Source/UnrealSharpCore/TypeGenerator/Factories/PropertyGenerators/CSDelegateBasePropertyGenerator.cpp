#include "CSDelegateBasePropertyGenerator.h"
#include "TypeGenerator/Factories/CSFunctionFactory.h"
#include "TypeGenerator/Register/MetaData/CSDelegatePropertyMetaData.h"

FProperty* UCSDelegateBasePropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FDelegateProperty* NewProperty = static_cast<FDelegateProperty*>(Super::CreateProperty(Outer, PropertyMetaData));
	TSharedPtr<FCSDelegatePropertyMetaData> DelegateMetaData = PropertyMetaData.GetTypeMetaData<FCSDelegatePropertyMetaData>();
	NewProperty->SignatureFunction = DelegateMetaData->Delegate.GetOwningDelegate();
	return NewProperty;
}

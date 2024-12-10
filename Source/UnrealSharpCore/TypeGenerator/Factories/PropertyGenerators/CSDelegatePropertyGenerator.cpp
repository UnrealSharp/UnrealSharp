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

TSharedPtr<FCSUnrealType> UCSDelegatePropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSDelegateMetaData>();
}

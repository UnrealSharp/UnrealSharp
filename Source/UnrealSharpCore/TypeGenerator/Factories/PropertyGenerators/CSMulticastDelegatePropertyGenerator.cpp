#include "CSMulticastDelegatePropertyGenerator.h"

#include "TypeGenerator/Factories/CSPropertyFactory.h"
#include "TypeGenerator/Register/MetaData/CSDelegateMetaData.h"

FProperty* UCSMulticastDelegatePropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FMulticastInlineDelegateProperty* NewProperty = static_cast<FMulticastInlineDelegateProperty*>(Super::CreateProperty(Outer, PropertyMetaData));
	UFunction* SignatureFunction = CreateSignatureFunction(PropertyMetaData);
	NewProperty->SignatureFunction = SignatureFunction;
	return NewProperty;
}

TSharedPtr<FCSUnrealType> UCSMulticastDelegatePropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSDelegateMetaData>();
}

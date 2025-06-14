#include "CSDelegatePropertyGenerator.h"

#include "TypeGenerator/Register/MetaData/CSDelegateMetaData.h"

FProperty* UCSDelegatePropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FDelegateProperty* NewProperty = static_cast<FDelegateProperty*>(Super::CreateProperty(Outer, PropertyMetaData));
	NewProperty->SignatureFunction = CreateSignatureFunction(PropertyMetaData);
	return NewProperty;
}

TSharedPtr<FCSUnrealType> UCSDelegatePropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSDelegateMetaData>();
}
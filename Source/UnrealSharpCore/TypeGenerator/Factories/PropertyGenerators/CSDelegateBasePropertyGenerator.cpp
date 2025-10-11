#include "CSDelegateBasePropertyGenerator.h"
#include "MetaData/FCSFieldTypePropertyMetaData.h"

FProperty* UCSDelegateBasePropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FDelegateProperty* NewProperty = static_cast<FDelegateProperty*>(Super::CreateProperty(Outer, PropertyMetaData));
	TSharedPtr<FCSFieldTypePropertyMetaData> DelegateMetaData = PropertyMetaData.GetTypeMetaData<FCSFieldTypePropertyMetaData>();
	NewProperty->SignatureFunction = DelegateMetaData->InnerType.GetAsDelegate();
	return NewProperty;
}

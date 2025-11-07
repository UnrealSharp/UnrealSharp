#include "Factories/PropertyGenerators/CSDelegateBasePropertyGenerator.h"
#include "MetaData/FCSFieldTypePropertyMetaData.h"

FProperty* UCSDelegateBasePropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FDelegateProperty* DelegateProperty = NewProperty<FDelegateProperty>(Outer, PropertyMetaData);
	TSharedPtr<FCSFieldTypePropertyMetaData> DelegateMetaData = PropertyMetaData.GetTypeMetaData<FCSFieldTypePropertyMetaData>();
	DelegateProperty->SignatureFunction = DelegateMetaData->InnerType.GetAsDelegate();
	return DelegateProperty;
}

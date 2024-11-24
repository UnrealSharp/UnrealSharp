#include "CSCommonPropertyGenerator.h"

bool UCSCommonPropertyGenerator::SupportsPropertyType(ECSPropertyType InPropertyType) const
{
	return TypeToFieldClass.Contains(InPropertyType);
}

FProperty* UCSCommonPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FFieldClass* FieldClass = TypeToFieldClass.FindChecked(PropertyMetaData.Type->PropertyType);
	return NewProperty(Outer, PropertyMetaData, FieldClass);
}

void UCSCommonPropertyGenerator::CreatePinInfoEditor(const FCSPropertyMetaData& PropertyMetaData,
	FEdGraphPinType& PinType)
{
	PinType.PinCategory = PropertyTypeToPinCategory.FindRef(PropertyMetaData.Type->PropertyType);
}

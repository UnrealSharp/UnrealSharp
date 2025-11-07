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

TSharedPtr<FCSUnrealType> UCSCommonPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	TSharedPtr<FCSUnrealType> MetaData;
	if (TFunction<TSharedPtr<FCSUnrealType>()>* FactoryMethod = MetaDataFactoryMap.Find(PropertyType))
	{
		MetaData = (*FactoryMethod)();
	}
	else
	{
		MetaData = MakeShared<FCSUnrealType>();
	}
	return MetaData;
}

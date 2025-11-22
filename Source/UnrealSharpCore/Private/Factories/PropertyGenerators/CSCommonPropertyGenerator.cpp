#include "Factories/PropertyGenerators/CSCommonPropertyGenerator.h"

#include "ReflectionData/CSUnrealType.h"

bool UCSCommonPropertyGenerator::SupportsPropertyType(ECSPropertyType InPropertyType) const
{
	return TypeToFieldClass.Contains(InPropertyType);
}

FProperty* UCSCommonPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData)
{
	FFieldClass* FieldClass = TypeToFieldClass.FindChecked(PropertyReflectionData.InnerType->PropertyType);
	return NewProperty(Outer, PropertyReflectionData, FieldClass);
}

TSharedPtr<FCSUnrealType> UCSCommonPropertyGenerator::CreatePropertyInnerTypeData(ECSPropertyType PropertyType)
{
	TSharedPtr<FCSUnrealType> InnerTypeData;
	if (TFunction<TSharedPtr<FCSUnrealType>()>* FactoryMethod = ReflectionDataFactoryMap.Find(PropertyType))
	{
		InnerTypeData = (*FactoryMethod)();
	}
	else
	{
		InnerTypeData = MakeShared<FCSUnrealType>();
	}
	
	return InnerTypeData;
}

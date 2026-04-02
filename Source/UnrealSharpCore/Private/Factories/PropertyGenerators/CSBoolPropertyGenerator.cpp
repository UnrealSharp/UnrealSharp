#include "Factories/PropertyGenerators/CSBoolPropertyGenerator.h"

#include "ReflectionData/CSUnrealType.h"

FProperty* UCSBoolPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData)
{
	FBoolProperty* BoolProperty = NewProperty<FBoolProperty>(Outer, PropertyReflectionData);
	BoolProperty->SetBoolSize(sizeof(bool), true);
	return BoolProperty;
}

TSharedPtr<FCSUnrealType> UCSBoolPropertyGenerator::CreatePropertyInnerTypeData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSUnrealType>();
}

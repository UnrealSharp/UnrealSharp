#include "Factories/PropertyGenerators/CSBoolPropertyGenerator.h"

FProperty* UCSBoolPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FBoolProperty* BoolProperty = NewProperty<FBoolProperty>(Outer, PropertyMetaData);
	BoolProperty->SetBoolSize(sizeof(bool), true);
	return BoolProperty;
}

TSharedPtr<FCSUnrealType> UCSBoolPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSUnrealType>();
}

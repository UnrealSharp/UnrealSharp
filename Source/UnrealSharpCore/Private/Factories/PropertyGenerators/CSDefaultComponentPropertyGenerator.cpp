#include "Factories/PropertyGenerators/CSDefaultComponentPropertyGenerator.h"

#include "Factories/PropertyGenerators/CSObjectPropertyGenerator.h"
#include "ReflectionData/CSDefaultComponentType.h"

TSharedPtr<FCSUnrealType> UCSDefaultComponentPropertyGenerator::CreatePropertyInnerTypeData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSDefaultComponentType>();
}

FProperty* UCSDefaultComponentPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData)
{
	UCSObjectPropertyGenerator* ObjectPropertyGenerator = GetMutableDefault<UCSObjectPropertyGenerator>();
	return ObjectPropertyGenerator->CreateProperty(Outer, PropertyReflectionData);
}

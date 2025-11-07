#include "Factories/PropertyGenerators/CSDefaultComponentPropertyGenerator.h"

#include "Factories/PropertyGenerators/CSObjectPropertyGenerator.h"
#include "MetaData/CSDefaultComponentMetaData.h"

TSharedPtr<FCSUnrealType> UCSDefaultComponentPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSDefaultComponentMetaData>();
}

FProperty* UCSDefaultComponentPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	UCSObjectPropertyGenerator* ObjectPropertyGenerator = GetMutableDefault<UCSObjectPropertyGenerator>();
	return ObjectPropertyGenerator->CreateProperty(Outer, PropertyMetaData);
}

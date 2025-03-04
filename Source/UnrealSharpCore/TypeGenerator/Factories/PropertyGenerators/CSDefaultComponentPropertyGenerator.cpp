#include "CSDefaultComponentPropertyGenerator.h"
#include "CSObjectPropertyGenerator.h"
#include "TypeGenerator/Register/MetaData/CSDefaultComponentMetaData.h"
#include "TypeGenerator/Register/MetaData/CSObjectMetaData.h"

TSharedPtr<FCSUnrealType> UCSDefaultComponentPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSDefaultComponentMetaData>();
}

FProperty* UCSDefaultComponentPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	UCSObjectPropertyGenerator* ObjectPropertyGenerator = GetMutableDefault<UCSObjectPropertyGenerator>();
	return ObjectPropertyGenerator->CreateProperty(Outer, PropertyMetaData);
}

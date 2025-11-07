#include "Factories/PropertyGenerators/CSDelegatePropertyGenerator.h"
#include "MetaData/FCSFieldTypePropertyMetaData.h"

TSharedPtr<FCSUnrealType> UCSDelegatePropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSFieldTypePropertyMetaData>();
}

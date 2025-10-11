#include "CSMulticastDelegatePropertyGenerator.h"
#include "TypeGenerator/Factories/CSPropertyFactory.h"
#include "MetaData/FCSFieldTypePropertyMetaData.h"

TSharedPtr<FCSUnrealType> UCSMulticastDelegatePropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSFieldTypePropertyMetaData>();
}

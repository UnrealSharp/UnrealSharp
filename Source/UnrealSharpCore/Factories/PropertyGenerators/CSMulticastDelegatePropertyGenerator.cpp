#include "CSMulticastDelegatePropertyGenerator.h"
#include "Factories/CSPropertyFactory.h"
#include "MetaData/FCSFieldTypePropertyMetaData.h"

TSharedPtr<FCSUnrealType> UCSMulticastDelegatePropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSFieldTypePropertyMetaData>();
}

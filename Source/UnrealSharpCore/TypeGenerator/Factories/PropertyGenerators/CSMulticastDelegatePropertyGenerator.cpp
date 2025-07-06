#include "CSMulticastDelegatePropertyGenerator.h"

#include "TypeGenerator/Factories/CSPropertyFactory.h"
#include "TypeGenerator/Register/MetaData/CSDelegatePropertyMetaData.h"

TSharedPtr<FCSUnrealType> UCSMulticastDelegatePropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSDelegatePropertyMetaData>();
}

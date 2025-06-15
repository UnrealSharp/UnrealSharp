#include "CSDelegatePropertyGenerator.h"

#include "TypeGenerator/Register/MetaData/CSDelegatePropertyMetaData.h"

TSharedPtr<FCSUnrealType> UCSDelegatePropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSDelegatePropertyMetaData>();
}
#include "Factories/PropertyGenerators/CSMapPropertyGenerator.h"
#include "Factories/CSPropertyFactory.h"
#include "MetaData/CSTemplateType.h"

FProperty* UCSMapPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FMapProperty* MapProperty = NewProperty<FMapProperty>(Outer, PropertyMetaData);

	TSharedPtr<FCSTemplateType> MapPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSTemplateType>();
	MapProperty->KeyProp = FCSPropertyFactory::CreateProperty(Outer, *MapPropertyMetaData->GetTemplateArgument(0));

	if (!CanBeHashed(MapProperty->KeyProp))
	{
		FText DialogText = FText::FromString(FString::Printf(TEXT("Data type cannot be used as a Key in %s.%s. Unsafe to use until fixed. Needs to be able to handle GetTypeHash."),
			*Outer->GetName(), *PropertyMetaData.GetName().ToString()));
		FMessageDialog::Open(EAppMsgType::Ok, DialogText);
	}
	
	MapProperty->KeyProp->Owner = MapProperty;
	MapProperty->ValueProp = FCSPropertyFactory::CreateProperty(Outer, *MapPropertyMetaData->GetTemplateArgument(1));
	MapProperty->ValueProp->Owner = MapProperty;
	return MapProperty;
}

TSharedPtr<FCSUnrealType> UCSMapPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSTemplateType>();
}

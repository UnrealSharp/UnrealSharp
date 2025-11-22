#include "Factories/PropertyGenerators/CSMapPropertyGenerator.h"
#include "Factories/CSPropertyFactory.h"
#include "ReflectionData/CSTemplateType.h"

FProperty* UCSMapPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData)
{
	FMapProperty* MapProperty = NewProperty<FMapProperty>(Outer, PropertyReflectionData);

	TSharedPtr<FCSTemplateType> TemplateData = PropertyReflectionData.GetInnerTypeData<FCSTemplateType>();
	MapProperty->KeyProp = FCSPropertyFactory::CreateProperty(Outer, *TemplateData->GetTemplateArgument(0));

	if (!CanBeHashed(MapProperty->KeyProp))
	{
		FText DialogText = FText::FromString(FString::Printf(TEXT("Data type cannot be used as a Key in %s.%s. Unsafe to use until fixed. Needs to be able to handle GetTypeHash."),
			*Outer->GetName(), *PropertyReflectionData.GetName().ToString()));
		FMessageDialog::Open(EAppMsgType::Ok, DialogText);
	}
	
	MapProperty->KeyProp->Owner = MapProperty;
	MapProperty->ValueProp = FCSPropertyFactory::CreateProperty(Outer, *TemplateData->GetTemplateArgument(1));
	MapProperty->ValueProp->Owner = MapProperty;
	return MapProperty;
}

TSharedPtr<FCSUnrealType> UCSMapPropertyGenerator::CreatePropertyInnerTypeData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSTemplateType>();
}

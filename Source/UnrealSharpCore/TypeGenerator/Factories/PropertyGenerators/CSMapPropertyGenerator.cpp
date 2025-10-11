#include "CSMapPropertyGenerator.h"
#include "TypeGenerator/Factories/CSPropertyFactory.h"
#include "MetaData/CSTemplateType.h"

FProperty* UCSMapPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FMapProperty* NewProperty = static_cast<FMapProperty*>(Super::CreateProperty(Outer, PropertyMetaData));

	TSharedPtr<FCSTemplateType> MapPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSTemplateType>();
	NewProperty->KeyProp = FCSPropertyFactory::CreateProperty(Outer, *MapPropertyMetaData->GetTemplateArgument(0));

	if (!CanBeHashed(NewProperty->KeyProp))
	{
		FText DialogText = FText::FromString(FString::Printf(TEXT("Data type cannot be used as a Key in %s.%s. Unsafe to use until fixed. Needs to be able to handle GetTypeHash."),
			*Outer->GetName(), *PropertyMetaData.GetName().ToString()));
		FMessageDialog::Open(EAppMsgType::Ok, DialogText);
	}
	
	NewProperty->KeyProp->Owner = NewProperty;
	NewProperty->ValueProp = FCSPropertyFactory::CreateProperty(Outer, *MapPropertyMetaData->GetTemplateArgument(1));
	NewProperty->ValueProp->Owner = NewProperty;
	return NewProperty;
}

TSharedPtr<FCSUnrealType> UCSMapPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSTemplateType>();
}

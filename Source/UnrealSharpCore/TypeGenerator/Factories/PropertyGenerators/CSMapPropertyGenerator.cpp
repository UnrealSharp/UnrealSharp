#include "CSMapPropertyGenerator.h"
#include "TypeGenerator/Factories/CSPropertyFactory.h"
#include "TypeGenerator/Register/MetaData/CSMapPropertyMetaData.h"

FProperty* UCSMapPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FMapProperty* NewProperty = static_cast<FMapProperty*>(Super::CreateProperty(Outer, PropertyMetaData));

	TSharedPtr<FCSMapPropertyMetaData> MapPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSMapPropertyMetaData>();
	NewProperty->KeyProp = FCSPropertyFactory::CreateProperty(Outer, MapPropertyMetaData->InnerProperty);

	if (!CanBeHashed(NewProperty->KeyProp))
	{
		FText DialogText = FText::FromString(FString::Printf(TEXT("Data type cannot be used as a Key in %s.%s. Unsafe to use until fixed. Needs to be able to handle GetTypeHash."),
			*Outer->GetName(), *PropertyMetaData.Name.ToString()));
		FMessageDialog::Open(EAppMsgType::Ok, DialogText);
	}
	
	NewProperty->KeyProp->Owner = NewProperty;
	NewProperty->ValueProp = FCSPropertyFactory::CreateProperty(Outer, MapPropertyMetaData->ValueType);
	NewProperty->ValueProp->Owner = NewProperty;
	return NewProperty;
}

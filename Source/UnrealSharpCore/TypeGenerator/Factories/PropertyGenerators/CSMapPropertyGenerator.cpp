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

TSharedPtr<FCSUnrealType> UCSMapPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSMapPropertyMetaData>();
}

#if WITH_EDITOR
FEdGraphPinType UCSMapPropertyGenerator::GetPinType(ECSPropertyType PropertyType, const FCSPropertyMetaData& MetaData, UBlueprint* Outer) const
{
	TSharedPtr<FCSMapPropertyMetaData> MapPropertyMetaData = MetaData.GetTypeMetaData<FCSMapPropertyMetaData>();

	ECSPropertyType KeyPropertyType = MapPropertyMetaData->InnerProperty.Type->PropertyType;
	UCSPropertyGenerator* KeyPropertyGenerator = FCSPropertyFactory::FindPropertyGenerator(KeyPropertyType);
	FEdGraphPinType PinType = KeyPropertyGenerator->GetPinType(KeyPropertyType, MapPropertyMetaData->InnerProperty, Outer);

	ECSPropertyType ValuePropertyType = MapPropertyMetaData->ValueType.Type->PropertyType;
	UCSPropertyGenerator* ValuePropertyGenerator = FCSPropertyFactory::FindPropertyGenerator(ValuePropertyType);
	FEdGraphPinType ValueType = ValuePropertyGenerator->GetPinType(ValuePropertyType, MapPropertyMetaData->ValueType, Outer);

	PinType.PinValueType.TerminalCategory = ValueType.PinCategory;
	PinType.PinValueType.TerminalSubCategoryObject = ValueType.PinSubCategoryObject;
	PinType.PinValueType.bTerminalIsWeakPointer = ValueType.bIsWeakPointer;
	PinType.PinValueType.bTerminalIsUObjectWrapper = ValueType.bIsUObjectWrapper;
	PinType.ContainerType = EPinContainerType::Map;
	return PinType;
}
#endif

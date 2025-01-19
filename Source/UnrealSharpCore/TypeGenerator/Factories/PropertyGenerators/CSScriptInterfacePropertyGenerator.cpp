#include "CSScriptInterfacePropertyGenerator.h"
#include "TypeGenerator/Register/CSTypeRegistry.h"
#include "TypeGenerator/Register/MetaData/CSObjectMetaData.h"

FProperty* UCSScriptInterfacePropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FInterfaceProperty* InterfaceProperty = static_cast<FInterfaceProperty*>(UCSPropertyGenerator::CreateProperty(Outer, PropertyMetaData));
	TSharedPtr<FCSObjectMetaData> InterfaceData = PropertyMetaData.GetTypeMetaData<FCSObjectMetaData>();
	UClass* InterfaceClass = FCSTypeRegistry::GetInterfaceFromName(InterfaceData->InnerType.Name);
	InterfaceProperty->SetInterfaceClass(InterfaceClass);
	return InterfaceProperty;
}

TSharedPtr<FCSUnrealType> UCSScriptInterfacePropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSObjectMetaData>();
}

#if WITH_EDITOR
FEdGraphPinType UCSScriptInterfacePropertyGenerator::GetPinType(ECSPropertyType PropertyType, const FCSPropertyMetaData& MetaData, UBlueprint* Outer) const
{
	TSharedPtr<FCSObjectMetaData> InterfaceData = MetaData.GetTypeMetaData<FCSObjectMetaData>();
	UClass* InterfaceClass = FCSTypeRegistry::GetInterfaceFromName(InterfaceData->InnerType.Name);

	FEdGraphPinType PinType;
	PinType.PinCategory = UEdGraphSchema_K2::PC_Interface;
	PinType.PinSubCategoryObject = InterfaceClass;
	return PinType;
}
#endif

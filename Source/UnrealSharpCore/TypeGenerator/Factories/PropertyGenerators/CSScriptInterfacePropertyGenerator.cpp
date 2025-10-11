#include "CSScriptInterfacePropertyGenerator.h"
#include "MetaData/FCSFieldTypePropertyMetaData.h"

FProperty* UCSScriptInterfacePropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FInterfaceProperty* InterfaceProperty = static_cast<FInterfaceProperty*>(UCSPropertyGenerator::CreateProperty(Outer, PropertyMetaData));
	
	TSharedPtr<FCSFieldTypePropertyMetaData> InterfaceData = PropertyMetaData.GetTypeMetaData<FCSFieldTypePropertyMetaData>();
	InterfaceProperty->SetInterfaceClass(InterfaceData->InnerType.GetAsInterface());
	
	return InterfaceProperty;
}

TSharedPtr<FCSUnrealType> UCSScriptInterfacePropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSFieldTypePropertyMetaData>();
}

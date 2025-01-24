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

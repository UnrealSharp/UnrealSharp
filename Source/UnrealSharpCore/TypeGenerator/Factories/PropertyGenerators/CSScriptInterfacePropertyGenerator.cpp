#include "CSScriptInterfacePropertyGenerator.h"
#include "TypeGenerator/Register/MetaData/CSObjectMetaData.h"

FProperty* UCSScriptInterfacePropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FInterfaceProperty* InterfaceProperty = static_cast<FInterfaceProperty*>(UCSPropertyGenerator::CreateProperty(Outer, PropertyMetaData));
	
	TSharedPtr<FCSObjectMetaData> InterfaceData = PropertyMetaData.GetTypeMetaData<FCSObjectMetaData>();
	InterfaceProperty->SetInterfaceClass(InterfaceData->InnerType.GetOwningInterface());
	
	return InterfaceProperty;
}

TSharedPtr<FCSUnrealType> UCSScriptInterfacePropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSObjectMetaData>();
}

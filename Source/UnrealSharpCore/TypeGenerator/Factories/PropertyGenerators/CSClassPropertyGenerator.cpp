#include "CSClassPropertyGenerator.h"
#include "TypeGenerator/Register/CSTypeRegistry.h"
#include "TypeGenerator/Register/MetaData/CSObjectMetaData.h"

FProperty* UCSClassPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FClassProperty* NewProperty = static_cast<FClassProperty*>(Super::CreateProperty(Outer, PropertyMetaData));

	TSharedPtr<FCSObjectMetaData> ObjectMetaData = PropertyMetaData.GetTypeMetaData<FCSObjectMetaData>();
	UClass* Class = FCSTypeRegistry::GetClassFromName(ObjectMetaData->InnerType.Name);
	
	NewProperty->PropertyClass = UClass::StaticClass();
	NewProperty->SetMetaClass(Class);
	return NewProperty;
}

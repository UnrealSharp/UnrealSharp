#include "CSClassPropertyGenerator.h"
#include "TypeGenerator/Register/MetaData/CSClassPropertyMetaData.h"
#include "TypeGenerator/Register/MetaData/CSObjectMetaData.h"

FProperty* UCSClassPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FClassProperty* NewProperty = static_cast<FClassProperty*>(Super::CreateProperty(Outer, PropertyMetaData));

	TSharedPtr<FCSObjectMetaData> ObjectMetaData = PropertyMetaData.GetTypeMetaData<FCSObjectMetaData>();
	UClass* Class = ObjectMetaData->InnerType.GetOwningClass();
	
	NewProperty->PropertyClass = UClass::StaticClass();
	NewProperty->SetMetaClass(Class);
	return NewProperty;
}

TSharedPtr<FCSUnrealType> UCSClassPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSClassPropertyMetaData>();
}

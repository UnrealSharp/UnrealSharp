#include "CSEnumPropertyGenerator.h"
#include "TypeGenerator/Register/CSTypeRegistry.h"
#include "TypeGenerator/Register/MetaData/CSEnumPropertyMetaData.h"

FProperty* UCSEnumPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FEnumProperty* NewProperty = static_cast<FEnumProperty*>(Super::CreateProperty(Outer, PropertyMetaData));
	const TSharedPtr<FCSEnumPropertyMetaData> EnumPropertyMetaData = PropertyMetaData.GetTypeMetaData<
		FCSEnumPropertyMetaData>();
	
	UEnum* Enum = FCSTypeRegistry::GetEnumFromName(EnumPropertyMetaData->InnerProperty.Name);
	FByteProperty* UnderlyingProp = new FByteProperty(NewProperty, "UnderlyingType", RF_Public);
	
	NewProperty->SetEnum(Enum);
	NewProperty->AddCppProperty(UnderlyingProp);
	
	return NewProperty;
}

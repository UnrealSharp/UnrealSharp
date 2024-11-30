#include "CSStructPropertyGenerator.h"
#include "TypeGenerator/Register/CSTypeRegistry.h"
#include "TypeGenerator/Register/MetaData/CSStructPropertyMetaData.h"

FProperty* UCSStructPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FStructProperty* StructProperty = static_cast<FStructProperty*>(Super::CreateProperty(Outer, PropertyMetaData));
	TSharedPtr<FCSStructPropertyMetaData> StructPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSStructPropertyMetaData>();
	StructProperty->Struct = FCSTypeRegistry::GetStructFromName(StructPropertyMetaData->TypeRef.Name);
	ensureAlways(StructProperty->Struct);
	return StructProperty;
}

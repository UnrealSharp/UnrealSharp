#include "CSStructPropertyGenerator.h"

#include "TypeGenerator/CSScriptStruct.h"
#include "TypeGenerator/Register/MetaData/CSStructPropertyMetaData.h"

FProperty* UCSStructPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FStructProperty* StructProperty = static_cast<FStructProperty*>(Super::CreateProperty(Outer, PropertyMetaData));
	TSharedPtr<FCSStructPropertyMetaData> StructPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSStructPropertyMetaData>();
	
	StructProperty->Struct = StructPropertyMetaData->TypeRef.GetOwningStruct();

	if (UCSScriptStruct* ManagedStruct = Cast<UCSScriptStruct>(StructProperty->Struct))
	{
		if (UStruct* OwningClass = TryFindingOwningClass(Outer))
		{
#if WITH_EDITORONLY_DATA
			ManagedStruct->ManagedReferences.AddReference(OwningClass);
#endif
		}
	}
	
	ensureAlways(StructProperty->Struct);
	return StructProperty;
}

TSharedPtr<FCSUnrealType> UCSStructPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSStructPropertyMetaData>();
}

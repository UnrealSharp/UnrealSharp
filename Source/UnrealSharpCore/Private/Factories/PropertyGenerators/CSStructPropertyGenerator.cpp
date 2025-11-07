#include "Factories/PropertyGenerators/CSStructPropertyGenerator.h"
#include "Types/CSScriptStruct.h"
#include "MetaData/FCSFieldTypePropertyMetaData.h"

FProperty* UCSStructPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FStructProperty* StructProperty = NewProperty<FStructProperty>(Outer, PropertyMetaData);
	
	TSharedPtr<FCSFieldTypePropertyMetaData> StructPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSFieldTypePropertyMetaData>();
	StructProperty->Struct = StructPropertyMetaData->InnerType.GetAsStruct();

#if WITH_EDITOR
	if (UCSScriptStruct* ManagedStruct = Cast<UCSScriptStruct>(StructProperty->Struct))
	{
		if (UStruct* OwningClass = TryFindingOwningClass(Outer))
		{
			ManagedStruct->GetManagedReferencesCollection().AddReference(OwningClass);
		}
	}
#endif

	return StructProperty;
}

TSharedPtr<FCSUnrealType> UCSStructPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSFieldTypePropertyMetaData>();
}

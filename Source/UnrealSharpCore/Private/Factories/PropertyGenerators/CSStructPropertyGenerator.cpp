#include "Factories/PropertyGenerators/CSStructPropertyGenerator.h"
#include "Types/CSScriptStruct.h"
#include "ReflectionData/CSFieldType.h"

FProperty* UCSStructPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData)
{
	FStructProperty* StructProperty = NewProperty<FStructProperty>(Outer, PropertyReflectionData);
	
	TSharedPtr<FCSFieldType> FieldType = PropertyReflectionData.GetInnerTypeData<FCSFieldType>();
	StructProperty->Struct = FieldType->InnerType.GetAsStruct();

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

TSharedPtr<FCSUnrealType> UCSStructPropertyGenerator::CreatePropertyInnerTypeData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSFieldType>();
}

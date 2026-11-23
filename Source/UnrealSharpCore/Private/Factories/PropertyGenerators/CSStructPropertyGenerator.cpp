#include "Factories/PropertyGenerators/CSStructPropertyGenerator.h"
#include "Types/CSScriptStruct.h"
#include "ReflectionData/CSFieldType.h"

FProperty* UCSStructPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData)
{
	TSharedPtr<FCSFieldType> FieldType = PropertyReflectionData.GetInnerTypeData<FCSFieldType>();
	UScriptStruct* Struct = FieldType->InnerType.ResolveField<UScriptStruct>();

#if WITH_EDITOR
	// A managed struct still pending recompilation must be rebuilt before a member takes its layout;
	// rebuilding it afterwards would move this member to a snapshot of the previous layout.
	if (UCSScriptStruct* PendingStruct = Cast<UCSScriptStruct>(Struct); PendingStruct && PendingStruct->HasManagedTypeDefinition())
	{
		PendingStruct->GetManagedTypeDefinition()->GetDefinition();
	}
#endif

	FStructProperty* StructProperty = NewProperty<FStructProperty>(Outer, PropertyReflectionData);
	StructProperty->Struct = Struct;

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

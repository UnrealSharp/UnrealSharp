#include "Factories/PropertyGenerators/CSEnumPropertyGenerator.h"
#include "CSManager.h"
#include "Types/CSEnum.h"
#include "ReflectionData/CSFieldType.h"

FProperty* UCSEnumPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData)
{
	FEnumProperty* EnumProperty = NewProperty<FEnumProperty>(Outer, PropertyReflectionData);
	const TSharedPtr<FCSFieldType> FieldType = PropertyReflectionData.GetInnerTypeData<FCSFieldType>();

	UCSManagedAssembly* Assembly = UCSManager::Get().FindAssembly(FieldType->InnerType.AssemblyName);
	UEnum* Enum = Assembly->ResolveUField<UEnum>(FieldType->InnerType.FieldName);

#if WITH_EDITOR
	if (UCSEnum* ManagedEnum = Cast<UCSEnum>(Enum))
	{
		if (UStruct* OwningClass = TryFindingOwningClass(Outer))
		{
			ManagedEnum->GetManagedReferencesCollection().AddReference(OwningClass);
		}
	}
#endif
	
	FByteProperty* UnderlyingProp = new FByteProperty(EnumProperty, "UnderlyingType", RF_Public);
	
	EnumProperty->SetEnum(Enum);
	EnumProperty->AddCppProperty(UnderlyingProp);
	
	return EnumProperty;
}

TSharedPtr<FCSUnrealType> UCSEnumPropertyGenerator::CreatePropertyInnerTypeData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSFieldType>();
}

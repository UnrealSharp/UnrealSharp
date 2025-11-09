#include "Factories/PropertyGenerators/CSEnumPropertyGenerator.h"
#include "CSManager.h"
#include "Types/CSEnum.h"
#include "MetaData/CSFieldTypePropertyMetaData.h"

FProperty* UCSEnumPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FEnumProperty* EnumProperty = NewProperty<FEnumProperty>(Outer, PropertyMetaData);
	const TSharedPtr<FCSFieldTypePropertyMetaData> EnumPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSFieldTypePropertyMetaData>();

	UCSAssembly* Assembly = UCSManager::Get().FindAssembly(EnumPropertyMetaData->InnerType.AssemblyName);
	UEnum* Enum = Assembly->FindType<UEnum>(EnumPropertyMetaData->InnerType.FieldName);

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

TSharedPtr<FCSUnrealType> UCSEnumPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSFieldTypePropertyMetaData>();
}

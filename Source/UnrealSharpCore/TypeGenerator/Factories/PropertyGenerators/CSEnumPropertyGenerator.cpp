#include "CSEnumPropertyGenerator.h"

#include "CSManager.h"
#include "TypeGenerator/CSEnum.h"
#include "MetaData/FCSFieldTypePropertyMetaData.h"

FProperty* UCSEnumPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FEnumProperty* NewProperty = static_cast<FEnumProperty*>(Super::CreateProperty(Outer, PropertyMetaData));
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
	
	FByteProperty* UnderlyingProp = new FByteProperty(NewProperty, "UnderlyingType", RF_Public);
	
	NewProperty->SetEnum(Enum);
	NewProperty->AddCppProperty(UnderlyingProp);
	
	return NewProperty;
}

TSharedPtr<FCSUnrealType> UCSEnumPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSFieldTypePropertyMetaData>();
}

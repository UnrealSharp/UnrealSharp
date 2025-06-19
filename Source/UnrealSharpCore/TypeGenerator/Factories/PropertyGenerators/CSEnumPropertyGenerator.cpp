#include "CSEnumPropertyGenerator.h"

#include "CSManager.h"
#include "TypeGenerator/CSEnum.h"
#include "TypeGenerator/Register/MetaData/CSEnumPropertyMetaData.h"

FProperty* UCSEnumPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FEnumProperty* NewProperty = static_cast<FEnumProperty*>(Super::CreateProperty(Outer, PropertyMetaData));
	const TSharedPtr<FCSEnumPropertyMetaData> EnumPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSEnumPropertyMetaData>();

	TSharedPtr<FCSAssembly> Assembly = UCSManager::Get().FindAssembly(EnumPropertyMetaData->InnerProperty.AssemblyName);
	UEnum* Enum = Assembly->FindEnum(EnumPropertyMetaData->InnerProperty.FieldName);

#if WITH_EDITOR
	if (UCSEnum* ManagedEnum = Cast<UCSEnum>(Enum))
	{
		if (UStruct* OwningClass = TryFindingOwningClass(Outer))
		{
			ManagedEnum->ManagedReferences.AddReference(OwningClass);
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
	return MakeShared<FCSEnumPropertyMetaData>();
}

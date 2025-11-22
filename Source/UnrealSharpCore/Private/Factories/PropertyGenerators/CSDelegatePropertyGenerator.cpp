#include "Factories/PropertyGenerators/CSDelegatePropertyGenerator.h"

#include "ReflectionData/CSTemplateType.h"
#include "ReflectionData/CSFieldType.h"

UCSDelegatePropertyGenerator::UCSDelegatePropertyGenerator(FObjectInitializer const& ObjectInitializer) : Super(ObjectInitializer)
{
	TypeToFieldClass =
	{
		{ ECSPropertyType::Delegate, FDelegateProperty::StaticClass() },
		{ ECSPropertyType::MulticastInlineDelegate, FMulticastInlineDelegateProperty::StaticClass() },
		{ ECSPropertyType::MulticastSparseDelegate, FMulticastInlineDelegateProperty::StaticClass() },
		{ ECSPropertyType::DelegateSignature, FDelegateProperty::StaticClass() }
	};

	REGISTER_REFLECTION_DATA(ECSPropertyType::Delegate, FCSTemplateType)
	REGISTER_REFLECTION_DATA(ECSPropertyType::MulticastInlineDelegate, FCSTemplateType)
	REGISTER_REFLECTION_DATA(ECSPropertyType::MulticastSparseDelegate, FCSTemplateType)
	REGISTER_REFLECTION_DATA(ECSPropertyType::DelegateSignature, FCSFieldType)
}

FProperty* UCSDelegatePropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData)
{
	FDelegateProperty* DelegateProperty = NewProperty<FDelegateProperty>(Outer, PropertyReflectionData, GetFieldClassForType(PropertyReflectionData));
	TSharedPtr<FCSTemplateType> TemplateType = PropertyReflectionData.GetInnerTypeData<FCSTemplateType>();
	
	const FCSPropertyReflectionData* InnerType = TemplateType->GetTemplateArgument(0);
	TSharedPtr<FCSFieldType> FieldType = InnerType->GetInnerTypeData<FCSFieldType>();
	
	DelegateProperty->SignatureFunction = FieldType->InnerType.GetAsDelegate();

	if (!IsValid(DelegateProperty->SignatureFunction))
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Failed to get delegate signature function for delegate property '{0}'", *PropertyReflectionData.FieldName.GetFullName().ToString());
		return nullptr;
	}
	
	return DelegateProperty;
}

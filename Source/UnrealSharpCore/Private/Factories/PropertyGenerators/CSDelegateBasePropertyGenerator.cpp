#include "Factories/PropertyGenerators/CSDelegateBasePropertyGenerator.h"

#include "MetaData/CSTemplateType.h"
#include "MetaData/CSFieldTypePropertyMetaData.h"

UCSDelegateBasePropertyGenerator::UCSDelegateBasePropertyGenerator(FObjectInitializer const& ObjectInitializer) : Super(ObjectInitializer)
{
	TypeToFieldClass =
	{
		{ ECSPropertyType::Delegate, FDelegateProperty::StaticClass() },
		{ ECSPropertyType::MulticastInlineDelegate, FMulticastInlineDelegateProperty::StaticClass() },
		{ ECSPropertyType::MulticastSparseDelegate, FMulticastInlineDelegateProperty::StaticClass() },
		{ ECSPropertyType::DelegateSignature, FDelegateProperty::StaticClass() }
	};

	REGISTER_METADATA(ECSPropertyType::Delegate, FCSTemplateType)
	REGISTER_METADATA(ECSPropertyType::MulticastInlineDelegate, FCSTemplateType)
	REGISTER_METADATA(ECSPropertyType::MulticastSparseDelegate, FCSTemplateType)
	REGISTER_METADATA(ECSPropertyType::DelegateSignature, FCSFieldTypePropertyMetaData)
}

FProperty* UCSDelegateBasePropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FDelegateProperty* DelegateProperty = NewProperty<FDelegateProperty>(Outer, PropertyMetaData, GetFieldClassForType(PropertyMetaData));
	TSharedPtr<FCSTemplateType> DelegateMetaData = PropertyMetaData.GetTypeMetaData<FCSTemplateType>();
	
	const FCSPropertyMetaData* InnerTypeMetaData = DelegateMetaData->GetTemplateArgument(0);
	TSharedPtr<FCSFieldTypePropertyMetaData> InnerFieldTypeMetaData = InnerTypeMetaData->GetTypeMetaData<FCSFieldTypePropertyMetaData>();
	
	DelegateProperty->SignatureFunction = InnerFieldTypeMetaData->InnerType.GetAsDelegate();

	if (!IsValid(DelegateProperty->SignatureFunction))
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Failed to get delegate signature function for delegate property '{0}'", *PropertyMetaData.FieldName.GetFullName().ToString());
		return nullptr;
	}
	
	return DelegateProperty;
}

#include "CSGeneratedTypeBuilder.h"
#include "CSAssembly.h"
#include "CSManager.h"
#include "CSMetaDataUtils.h"
#include "TypeInfo/CSManagedTypeInfo.h"

UField* UCSGeneratedTypeBuilder::CreateType(const TSharedPtr<FCSManagedTypeInfo>& ManagedTypeInfo) const
{
	TSharedPtr<const FCSTypeReferenceMetaData> TypeMetaData = ManagedTypeInfo->GetTypeMetaData<FCSTypeReferenceMetaData>();
	UPackage* Package = UCSManager::Get().GetPackage(TypeMetaData->FieldName.GetNamespace());
	FString FieldName = GetFieldName(ManagedTypeInfo);

	UField* FieldToBuild;
#if WITH_EDITOR
	FieldToBuild = FindObject<UField>(Package, *FieldName);
	if (!IsValid(FieldToBuild))
#endif
	{
		FieldToBuild = NewObject<UField>(Package, GetFieldType(), *FieldName, RF_Public | RF_Standalone);
	}

	if (ICSManagedTypeInterface* ManagedTypeInterface = Cast<ICSManagedTypeInterface>(FieldToBuild))
	{
		ManagedTypeInterface->SetTypeInfo(ManagedTypeInfo);
	}

#if WITH_EDITOR
	FCSMetaDataUtils::ApplyMetaData(TypeMetaData->MetaData, FieldToBuild);
#endif
		
	return FieldToBuild;
}

FString UCSGeneratedTypeBuilder::GetFieldName(const TSharedPtr<FCSManagedTypeInfo>& ManagedTypeInfo) const
{
	TSharedPtr<FCSTypeReferenceMetaData> MetaData = ManagedTypeInfo->GetTypeMetaData();
	return FCSMetaDataUtils::GetAdjustedFieldName(MetaData->FieldName);
}

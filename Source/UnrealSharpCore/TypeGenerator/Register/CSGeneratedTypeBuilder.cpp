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
		UClass* FieldType = GetFieldType();

		if (!IsValid(FieldType))
		{
			UE_LOGFMT(LogUnrealSharp, Fatal, "Field type is not set for {0}.", *FieldName);
			return nullptr;
		}
		
		FieldToBuild = NewObject<UField>(Package, FieldType, *FieldName, RF_Public | RF_Standalone);
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
	return FCSMetaDataUtils::GetAdjustedFieldName(ManagedTypeInfo->GetTypeMetaData<FCSTypeReferenceMetaData>()->FieldName);
}

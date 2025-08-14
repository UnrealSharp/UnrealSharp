#include "CSGeneratedTypeBuilder.h"
#include "CSAssembly.h"
#include "CSManager.h"
#include "CSMetaDataUtils.h"
#include "TypeInfo/CSManagedTypeInfo.h"

UField* UCSGeneratedTypeBuilder::CreateType()
{
	TSharedPtr<const FCSTypeReferenceMetaData> TypeMetaData = ManagedTypeInfo->GetTypeMetaData<FCSTypeReferenceMetaData>();
	
	UPackage* Package =  UCSManager::Get().GetPackage(TypeMetaData->FieldName.GetNamespace());
	FName FieldName = GetFieldName();

#if WITH_EDITOR
	FieldToBuild = FindObject<UField>(Package, *FieldName.ToString());
	if (!IsValid(FieldToBuild))
#endif
	{
		UClass* FieldType = GetFieldType();

		if (!IsValid(FieldType))
		{
			UE_LOGFMT(LogUnrealSharp, Fatal, "Field type is not set for {0}.", *FieldName.ToString());
			return nullptr;
		}
		
		FieldToBuild = NewObject<UField>(Package, FieldType, FieldName, RF_Public | RF_Standalone);
	}

	if (ICSManagedTypeInterface* ManagedTypeInterface = Cast<ICSManagedTypeInterface>(FieldToBuild))
	{
		ManagedTypeInterface->SetTypeInfo(ManagedTypeInfo);
	}
		
	return FieldToBuild;
}

FName UCSGeneratedTypeBuilder::GetFieldName() const
{
	return FCSMetaDataUtils::GetAdjustedFieldName(ManagedTypeInfo->GetTypeMetaData<FCSTypeReferenceMetaData>()->FieldName);
}

UCSAssembly* UCSGeneratedTypeBuilder::GetOwningAssembly() const
{
	return ManagedTypeInfo->GetOwningAssembly();
}

#include "CSGeneratedTypeBuilder.h"
#include "CSAssembly.h"
#include "CSManager.h"
#include "CSMetaDataUtils.h"
#include "TypeInfo/CSManagedTypeInfo.h"

UField* UCSGeneratedTypeBuilder::CreateField(const TSharedPtr<FCSManagedTypeInfo>& ManagedTypeInfo) const
{
	if (!ManagedTypeInfo.IsValid())
	{
		UE_LOGFMT(LogUnrealSharp, Error, "ManagedTypeInfo is invalid, cannot create type.");
		return nullptr;
	}
	
	UField* ExistingType = ManagedTypeInfo->GetField();
	if (IsValid(ExistingType))
	{
		UE_LOGFMT(LogUnrealSharp, Warning, "Type: {0} already exists, skipping creation.", *ExistingType->GetName());
		return ExistingType;
	}
	
	TSharedPtr<const FCSTypeReferenceMetaData> TypeMetaData = ManagedTypeInfo->GetTypeMetaData<FCSTypeReferenceMetaData>();
	UPackage* OwningPackage = UCSManager::Get().GetPackage(TypeMetaData->FieldName.GetNamespace());
	FString Name = GetFieldName(TypeMetaData);

	UField* NewField = NewObject<UField>(OwningPackage, FieldType, *Name, RF_Public);
	
	if (ICSManagedTypeInterface* ManagedTypeInterface = Cast<ICSManagedTypeInterface>(NewField))
	{
		ManagedTypeInterface->SetTypeInfo(ManagedTypeInfo);
	}

	UE_LOGFMT(LogUnrealSharp, VeryVerbose, "Created type: {0} in package: {1}", *Name, *OwningPackage->GetName());
	return NewField;
}

void UCSGeneratedTypeBuilder::TriggerRebuild(UField* FieldToRebuild, const TSharedPtr<FCSManagedTypeInfo>& ManagedTypeInfo) const
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSGeneratedTypeBuilder::StartRebuildingType);
	UE_LOGFMT(LogUnrealSharp, VeryVerbose, "Rebuilding type: {0}", *FieldToRebuild->GetName());
	
	RebuildType(FieldToRebuild, ManagedTypeInfo);

#if !WITH_EDITOR
	// Apply metadata only in packaged builds, in editor we apply it in the FCSCompilerContext::FinishCompilingClass
	TSharedPtr<const FCSTypeReferenceMetaData> TypeMetaData = ManagedTypeInfo->GetTypeMetaData<FCSTypeReferenceMetaData>();	
	FCSMetaDataUtils::ApplyMetaData(TypeMetaData->MetaData, FieldToRebuild);
#endif
}

FString UCSGeneratedTypeBuilder::GetFieldName(TSharedPtr<const FCSTypeReferenceMetaData>& MetaData) const
{
	return FCSMetaDataUtils::GetAdjustedFieldName(MetaData->FieldName);
}

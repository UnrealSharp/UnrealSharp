#include "Utilities/CSMetaDataUtils.h"
#include "CSUnrealSharpSettings.h"
#include "ReflectionData/CSTypeReferenceReflectionData.h"

void FCSMetaDataUtils::ApplyMetaData(const TArray<FCSMetaDataEntry>& MetaDataMap, UField* Field)
{
#if WITH_EDITOR
	for (const FCSMetaDataEntry& MetaData : MetaDataMap)
	{
		Field->SetMetaData(*MetaData.Key, *MetaData.Value);
	}
#endif
}

void FCSMetaDataUtils::ApplyBaseMetaData(UField* Field)
{
#if WITH_EDITOR
	Field->SetMetaData(TEXT("BlueprintType"), TEXT("true"));
#endif
}

void FCSMetaDataUtils::ApplyMetaData(const TArray<FCSMetaDataEntry>& MetaDataMap, FField* Field)
{
#if WITH_EDITOR
	for (const FCSMetaDataEntry& MetaData : MetaDataMap)
	{
		Field->SetMetaData(*MetaData.Key, *MetaData.Value);
	}
#endif
}

FString FCSMetaDataUtils::GetAdjustedFieldName(const FCSFieldName& FieldName)
{
	FString Name;
	if (GetDefault<UCSUnrealSharpSettings>()->HasNamespaceSupport())
	{
		FString Namespace = FieldName.GetNamespace().GetName();
		FString EngineName = FieldName.GetEngineName();
		
		Name = FString::Printf(TEXT("%s.%s"), *Namespace, *EngineName);
		Name.ReplaceInline(TEXT("."), TEXT("_"));
	}
	else
	{
		Name = FieldName.GetEngineName();
	}

	return *Name;
}

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
		Name = FieldName.GetFullName().ToString();

		// Unreal doesn't really consider dots in names for editor display. So we replace them with underscores.
		Name.ReplaceInline(TEXT("."), TEXT("_"));
	}
	else
	{
		Name = FieldName.GetName();
	}

	return *Name;
}

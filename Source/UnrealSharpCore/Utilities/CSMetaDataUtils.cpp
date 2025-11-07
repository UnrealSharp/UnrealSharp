#include "CSMetaDataUtils.h"
#include "CSUnrealSharpSettings.h"

void FCSMetaDataUtils::ApplyMetaData(const TMap<FString, FString>& MetaDataMap, UField* Field)
{
#if WITH_EDITOR
	for (const TPair<FString, FString>& MetaData : MetaDataMap)
	{
		Field->SetMetaData(*MetaData.Key, *MetaData.Value);
	}
#endif
}

void FCSMetaDataUtils::ApplyMetaData(const TMap<FString, FString>& MetaDataMap, FField* Field)
{
#if WITH_EDITOR
	for (const TPair<FString, FString>& MetaData : MetaDataMap)
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

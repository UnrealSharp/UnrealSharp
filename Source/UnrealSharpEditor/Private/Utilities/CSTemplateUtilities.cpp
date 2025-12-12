#include "Utilities/CSTemplateUtilities.h"
#include "CSProcUtilities.h"

DEFINE_LOG_CATEGORY_STATIC(LogCSTemplateUtilities, Log, All);

const FString& FCSTemplateUtilities::GetTemplateFolderPath()
{
	static FString TemplateFolderPath = UCSProcUtilities::GetPluginDirectory() / TEXT("Templates");
	return TemplateFolderPath;
}

bool FCSTemplateUtilities::FillTemplateFile(const FString& TemplateName, TMap<FString, FString>& Replacements, const FString& Path)
{
	const FString FullFileName = GetTemplateFolderPath() / TemplateName + TEXT(".cs.template");

	FString OutTemplate;
	if (!FFileHelper::LoadFileToString(OutTemplate, *FullFileName))
	{
		UE_LOG(LogCSTemplateUtilities, Error, TEXT("Failed to load template file %s"), *FullFileName);
		return false;
	}

	for (const TPair<FString, FString>& Replacement : Replacements)
	{
		FString ReplacementKey = TEXT("%") + Replacement.Key + TEXT("%");
		OutTemplate = OutTemplate.Replace(*ReplacementKey, *Replacement.Value);
	}

	if (!FFileHelper::SaveStringToFile(OutTemplate, *Path))
	{
		UE_LOG(LogCSTemplateUtilities, Error, TEXT("Failed to save %s when trying to create a template"), *Path);
		return false;
	}

	return true;
}

#pragma once

DECLARE_LOG_CATEGORY_EXTERN(LogFCSTemplateUtilities, Log, All);

namespace FCSTemplateUtilities
{
	const FString& GetTemplateFolderPath();
	bool FillTemplateFile(const FString& TemplateName, TMap<FString, FString>& Replacements, const FString& Path);
};

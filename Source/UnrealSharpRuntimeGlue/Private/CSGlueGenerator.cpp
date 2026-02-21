#include "CSGlueGenerator.h"
#include "UnrealSharpRuntimeGlue.h"
#include "Logging/StructuredLog.h"
#include "CSProcUtilities.h"

FString UCSGlueGenerator::GetPluginGlueFolder(const FString& PluginName)
{
	return UCSProcUtilities::GetPluginGlueFolderPath(PluginName);
}

void UCSGlueGenerator::SaveRuntimeGlue(const FCSScriptBuilder& ScriptBuilder, const FString& FileName, const FString* OverrideFolder, const FString& Suffix)
{
	FString Path = OverrideFolder ? *OverrideFolder : UCSProcUtilities::GetProjectGlueFolderPath();
	Path /= FileName + Suffix;

	FString CurrentRuntimeGlue;
	FFileHelper::LoadFileToString(CurrentRuntimeGlue, *Path);

	if (CurrentRuntimeGlue == ScriptBuilder.ToString())
	{
		// No changes, return
		return;
	}

	if (!FFileHelper::SaveStringToFile(ScriptBuilder.ToString(), *Path))
	{
		UE_LOGFMT(LogUnrealSharpRuntimeGlue, Error, "Failed to save runtime glue to {0}", *Path);
		return;
	}

	UE_LOGFMT(LogUnrealSharpRuntimeGlue, Display, "Saved {0}", *FileName);
	FUnrealSharpRuntimeGlueModule::Get().GetOnRuntimeGlueChanged().Broadcast(this, Path);
}

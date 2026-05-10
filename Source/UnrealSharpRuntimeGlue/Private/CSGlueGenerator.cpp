#include "CSGlueGenerator.h"

#include "CSPathsUtilities.h"
#include "UnrealSharpRuntimeGlue.h"
#include "Logging/StructuredLog.h"

FString UCSGlueGenerator::GetPluginGlueFolder(const FString& PluginName)
{
	return UnrealSharp::Paths::GetPluginGlueFolderPath(PluginName);
}

void UCSGlueGenerator::SaveRuntimeGlue(FCSScriptBuilder& ScriptBuilder, const FString& FileName, const FString& Suffix)
{
	FString FullFileName = FileName + Suffix;
	FString Path = FPaths::Combine(FUnrealSharpRuntimeGlueModule::GetGlueFolder(), FullFileName);

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

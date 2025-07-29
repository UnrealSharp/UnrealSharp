#include "CSGlueGenerator.h"
#include "UnrealSharpRuntimeGlue.h"
#include "UnrealSharpProcHelper/CSProcHelper.h"

void UCSGlueGenerator::SaveRuntimeGlue(const FCSScriptBuilder& ScriptBuilder, const FString& FileName, const FString& Suffix)
{
	const FString Path = FPaths::Combine(FCSProcHelper::GetProjectGlueFolderPath(), FileName + Suffix);

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

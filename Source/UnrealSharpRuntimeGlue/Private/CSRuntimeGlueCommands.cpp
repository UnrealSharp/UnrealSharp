#include "CSRuntimeGlueCommands.h"

#include "CSStyle.h"

#define LOCTEXT_NAMESPACE "FCSCommandsModule"

FCSRuntimeGlueCommands::FCSRuntimeGlueCommands() : TCommands<FCSRuntimeGlueCommands>(
	TEXT("CSRuntimeGlueCommands"),
	NSLOCTEXT("Contexts", "UnrealSharpCommands", "Runtime Glue Commands"),
	NAME_None,
	FAppStyle::GetAppStyleSetName())
{
	
}

void FCSRuntimeGlueCommands::RegisterCommands()
{
	UI_COMMAND(RefreshRuntimeGlue, "Refresh Runtime Glue", "Refresh the generated runtime glue such as the GameplayTags, AssetIds, AssetTypes, TraceChannel", EUserInterfaceActionType::Button, FInputChord());
}

#undef LOCTEXT_NAMESPACE

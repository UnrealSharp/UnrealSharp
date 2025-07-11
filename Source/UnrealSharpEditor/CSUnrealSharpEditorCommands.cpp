#include "CSUnrealSharpEditorCommands.h"

#include "CSStyle.h"

#define LOCTEXT_NAMESPACE "FCSCommandsModule"

FCSUnrealSharpEditorCommands::FCSUnrealSharpEditorCommands() : TCommands<FCSUnrealSharpEditorCommands>(
	TEXT("CSCommands"),
	NSLOCTEXT("Contexts", "UnrealSharpCommands", "UnrealSharp"),
	NAME_None,
	FCSStyle::GetStyleSetName())
{
	
}

void FCSUnrealSharpEditorCommands::RegisterCommands()
{
	UI_COMMAND(CreateNewProject, "Create C# Project", "Create a new C# project with all necessary dependencies and initial setup", EUserInterfaceActionType::Button, FInputChord());
	UI_COMMAND(CompileManagedCode, "Force Compile C#", "Trigger a hot reload to recompile the project's C# code", EUserInterfaceActionType::Button, FInputChord(EKeys::F10, EModifierKey::Control | EModifierKey::Alt));
	UI_COMMAND(ReloadManagedCode, "Force Reload Modules", "Reloads the built modules in case they were built externally (for example from your IDE)", EUserInterfaceActionType::Button, FInputChord(EKeys::F9, EModifierKey::Control | EModifierKey::Alt));
	UI_COMMAND(RegenerateSolution, "Regenerate Solution", "Rebuild the C# solution file to reflect the latest project changes", EUserInterfaceActionType::Button, FInputChord());
	UI_COMMAND(OpenSolution, "Open C# Solution", "Launch the project's C# solution file in the default IDE", EUserInterfaceActionType::Button, FInputChord());
	UI_COMMAND(MergeManagedSlnAndNativeSln, "Merge Managed and Native Solution", "Merges the managed sln and native sln into one mixed.sln, coding in one IDE instance. This will create a new sln in the root folder of your project", EUserInterfaceActionType::Button, FInputChord());
	UI_COMMAND(PackageProject, "Package Project", "Package the C# project to the archived directory", EUserInterfaceActionType::Button, FInputChord());
	UI_COMMAND(OpenSettings, "Open Settings...", "Open the Editor Settings", EUserInterfaceActionType::Button, FInputChord());
	UI_COMMAND(OpenDocumentation, "Open Documentation", "Open the Documentation website", EUserInterfaceActionType::Button, FInputChord());
	UI_COMMAND(ReportBug, "Report a Bug", "Open the Issues Github page", EUserInterfaceActionType::Button, FInputChord());
	UI_COMMAND(RefreshRuntimeGlue, "Refresh Runtime Glue", "Refresh the generated runtime glue such as the GameplayTags, AssetIds, AssetTypes, TraceChannel", EUserInterfaceActionType::Button, FInputChord());
	UI_COMMAND(RepairComponents, "Repair Components", "Transfers data from the old component system to the new one. This tool is only relevant if you see double instances of the same component in your BPs.", EUserInterfaceActionType::Button, FInputChord());
}

#undef LOCTEXT_NAMESPACE

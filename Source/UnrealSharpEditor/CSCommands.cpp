#include "CSCommands.h"

#include "CSStyle.h"

#define LOCTEXT_NAMESPACE "FCSCommandsModule"

FCSCommands::FCSCommands() : TCommands<FCSCommands>(
	TEXT("CSCommands"),
	NSLOCTEXT("Contexts", "UnrealSharpCommands", "UnrealSharp"),
	NAME_None,
	FCSStyle::GetStyleSetName())
{
	
}

void FCSCommands::RegisterCommands()
{
	UI_COMMAND(CreateNewProject, "New C# Project", "Create a new C# project with all necessary dependencies and initial setup", EUserInterfaceActionType::Button, FInputChord());
	UI_COMMAND(CompileManagedCode, "Compile C#", "Trigger a hot reload to recompile the project's C# code", EUserInterfaceActionType::Button, FInputChord());
	UI_COMMAND(RegenerateSolution, "Regenerate Solution", "Rebuild the C# solution file to reflect the latest project changes", EUserInterfaceActionType::Button, FInputChord());
	UI_COMMAND(OpenSolution, "Open C# Solution", "Launch the project's C# solution file in the default IDE", EUserInterfaceActionType::Button, FInputChord());
	UI_COMMAND(PackageProject, "Package Project", "Package the C# project to the archived directory", EUserInterfaceActionType::Button, FInputChord());
	UI_COMMAND(OpenSettings, "Open Settings...", "Open the Editor Settings", EUserInterfaceActionType::Button, FInputChord());
	UI_COMMAND(OpenDocumentation, "Open Documentation", "Open the Documentation website", EUserInterfaceActionType::Button, FInputChord());
	UI_COMMAND(ReportBug, "Report a Bug", "Open the Issues Github page", EUserInterfaceActionType::Button, FInputChord());
}

#undef LOCTEXT_NAMESPACE

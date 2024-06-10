#include "CSharpCompileCommand.h"


#define LOCTEXT_NAMESPACE "FUnrealSharpEditorModule"

void FCSharpCompileCommand::RegisterCommands()
{
	UI_COMMAND(PluginAction, "Compile c#", "Compile c#", EUserInterfaceActionType::Button, FInputChord());
}

#pragma once

#include "CoreMinimal.h"
#include "Framework/Commands/Commands.h"

class FCSharpCompileCommand : public TCommands<FCSharpCompileCommand>
{
public:

	FCSharpCompileCommand()
		: TCommands<FCSharpCompileCommand>(TEXT("Compile C#"), NSLOCTEXT("Contexts", "BSeomText", "BSeomText Plugin"), NAME_None, FName(*FString("TODO")))
	{
	}

	// TCommands<> interface
	virtual void RegisterCommands() override;

public:
	TSharedPtr< FUICommandInfo > PluginAction;
};

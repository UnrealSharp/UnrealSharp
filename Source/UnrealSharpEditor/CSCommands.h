#pragma once
#include "EditorStyleSet.h"

class CSCommands
{
public:
	
};

class FCSCommands : public TCommands<FCSCommands>
{
public:

	FCSCommands();

	// TCommands<> interface
	virtual void RegisterCommands() override;
	// End

public:
	TSharedPtr<FUICommandInfo> CreateNewProject;
	TSharedPtr<FUICommandInfo> CompileManagedCode;
	TSharedPtr<FUICommandInfo> RegenerateSolution;
	TSharedPtr<FUICommandInfo> OpenSolution;
};


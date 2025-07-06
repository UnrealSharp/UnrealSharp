#pragma once

class CSCommands
{
};

class FCSCommands : public TCommands<FCSCommands>
{
public:
	FCSCommands();

	// TCommands<> interface
	virtual void RegisterCommands() override;
	// End

	TSharedPtr<FUICommandInfo> CreateNewProject;
	TSharedPtr<FUICommandInfo> CompileManagedCode;
	TSharedPtr<FUICommandInfo> ReloadManagedCode;
	TSharedPtr<FUICommandInfo> RegenerateSolution;
	TSharedPtr<FUICommandInfo> OpenSolution;
	TSharedPtr<FUICommandInfo> MergeManagedSlnAndNativeSln;
	TSharedPtr<FUICommandInfo> PackageProject;
	TSharedPtr<FUICommandInfo> OpenSettings;
	TSharedPtr<FUICommandInfo> OpenDocumentation;
	TSharedPtr<FUICommandInfo> ReportBug;
	TSharedPtr<FUICommandInfo> RefreshRuntimeGlue;
	TSharedPtr<FUICommandInfo> RepairComponents;
};


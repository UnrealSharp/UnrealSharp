#pragma once

class FCSUnrealSharpEditorCommands : public TCommands<FCSUnrealSharpEditorCommands>
{
public:
	FCSUnrealSharpEditorCommands();

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


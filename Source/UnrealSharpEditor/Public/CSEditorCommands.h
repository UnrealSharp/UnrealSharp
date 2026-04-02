#pragma once

class FCSEditorCommands : public TCommands<FCSEditorCommands>
{
public:
	FCSEditorCommands();

	// TCommands<> interface
	virtual void RegisterCommands() override;
	// End

	TSharedPtr<FUICommandInfo> CreateNewProject;
    TSharedPtr<FUICommandInfo> HotReload;
	TSharedPtr<FUICommandInfo> RegenerateSolution;
	TSharedPtr<FUICommandInfo> OpenSolution;
	TSharedPtr<FUICommandInfo> MergeManagedSlnAndNativeSln;
	TSharedPtr<FUICommandInfo> PackageProject;
	TSharedPtr<FUICommandInfo> OpenSettings;
	TSharedPtr<FUICommandInfo> OpenDocumentation;
	TSharedPtr<FUICommandInfo> ReportBug;
};


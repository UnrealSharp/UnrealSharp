#pragma once

class FCSUnrealSharpEditorCommands : public TCommands<FCSUnrealSharpEditorCommands>
{
public:
	FCSUnrealSharpEditorCommands();

	// TCommands<> interface
	virtual void RegisterCommands() override;
	// End

	TSharedPtr<FUICommandInfo> CreateNewProject;
    TSharedPtr<FUICommandInfo> HotReload;
    TSharedPtr<FUICommandInfo> HotReloadAssemblyOnly;
	TSharedPtr<FUICommandInfo> RegenerateSolution;
	TSharedPtr<FUICommandInfo> OpenSolution;
	TSharedPtr<FUICommandInfo> MergeManagedSlnAndNativeSln;
	TSharedPtr<FUICommandInfo> PackageProject;
	TSharedPtr<FUICommandInfo> OpenSettings;
	TSharedPtr<FUICommandInfo> OpenDocumentation;
	TSharedPtr<FUICommandInfo> ReportBug;
	TSharedPtr<FUICommandInfo> RefreshRuntimeGlue;
};


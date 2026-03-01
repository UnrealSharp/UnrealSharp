#pragma once

class FCSRuntimeGlueCommands : public TCommands<FCSRuntimeGlueCommands>
{
public:
	FCSRuntimeGlueCommands();

	// TCommands<> interface
	virtual void RegisterCommands() override;
	// End
	
	TSharedPtr<FUICommandInfo> RefreshRuntimeGlue;
};


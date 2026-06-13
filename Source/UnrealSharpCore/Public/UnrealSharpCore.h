#pragma once

#include "CoreMinimal.h"
#include "DotNet/CSDotNetRuntimeHost.h"
#include "Modules/ModuleManager.h"

DECLARE_LOG_CATEGORY_EXTERN(LogUnrealSharp, Log, All);

class FUnrealSharpCoreModule : public IModuleInterface
{
public:
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;
private:
	FCSDotNetRuntimeHost DotNetRuntimeHost;
};

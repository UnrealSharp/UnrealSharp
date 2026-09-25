#include "UnrealSharpCore.h"
#include "CoreMinimal.h"
#include "CSManager.h"
#include "CSDialogUtilities.h"
#include "CSDotnetUtilties.h"
#include "Logging/StructuredLog.h"
#include "Properties/CSPropertyGeneratorManager.h"
#include "Modules/ModuleManager.h"


#define LOCTEXT_NAMESPACE "FUnrealSharpCoreModule"

DEFINE_LOG_CATEGORY(LogUnrealSharp);

void FUnrealSharpCoreModule::StartupModule()
{
#if WITH_EDITOR
	while (!UnrealSharp::DotNetUtilities::VerifyCSharpEnvironment() || !UnrealSharp::DotNetUtilities::BuildUserSolution())
	{
		if (UnrealSharp::Dialogs::IsHeadless())
		{
			UE_LOGFMT(LogUnrealSharp, Error, "UnrealSharp could not be initialized, see the errors above. Exiting.");
			FPlatformMisc::RequestExitWithStatus(true, 1);
			return;
		}
	}
#endif
	
	if (!DotNetRuntimeHost.InitializeManagedRuntime())
	{
		return;
	}
	
	UCSManager::Get().Initialize();
}

void FUnrealSharpCoreModule::ShutdownModule()
{
	FCSPropertyGeneratorManager::Shutdown();
}

#undef LOCTEXT_NAMESPACE
	
IMPLEMENT_MODULE(FUnrealSharpCoreModule, UnrealSharpCore)
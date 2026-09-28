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

#if WITH_EDITOR
/**
 * Checks the .NET SDK and builds the user's C# projects. Tries once: on failure the check or the build has already
 * shown a dialog, and re-running it without the user changing anything would just show that same dialog again.
 * Interactive editors keep running with UnrealSharp disabled for the session; headless editors can't leave a broken
 * environment sitting there, so they exit with code 1.
 */
static bool PrepareUserCSharpCode()
{
	if (UnrealSharp::DotNetUtilities::VerifyCSharpEnvironment() && UnrealSharp::DotNetUtilities::BuildUserSolution())
	{
		return true;
	}

	if (UnrealSharp::Dialogs::IsHeadless())
	{
		UE_LOGFMT(LogUnrealSharp, Error, "UnrealSharp could not be initialized, see the errors above. Exiting.");
		FPlatformMisc::RequestExitWithStatus(true, 1);
	}

	return false;
}
#endif

void FUnrealSharpCoreModule::StartupModule()
{
#if WITH_EDITOR
	if (!PrepareUserCSharpCode())
	{
		return;
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
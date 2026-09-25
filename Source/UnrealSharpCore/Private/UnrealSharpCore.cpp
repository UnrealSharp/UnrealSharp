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
 * Checks the .NET SDK and builds the user's C# projects. Interactive editors keep retrying, since each failure shows a
 * dialog and the user can fix the problem before closing it (or cancel a failed build, which exits the editor).
 * Headless editors cannot wait for that, so they exit with code 1 after the first failure.
 */
static bool PrepareUserCSharpCode()
{
	while (true)
	{
		if (UnrealSharp::DotNetUtilities::VerifyCSharpEnvironment() && UnrealSharp::DotNetUtilities::BuildUserSolution())
		{
			return true;
		}

		if (UnrealSharp::Dialogs::IsHeadless())
		{
			UE_LOGFMT(LogUnrealSharp, Error, "UnrealSharp could not be initialized, see the errors above. Exiting.");
			FPlatformMisc::RequestExitWithStatus(true, 1);
			return false;
		}
	}
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
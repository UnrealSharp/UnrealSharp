#include "UnrealSharpCore.h"
#include "CoreMinimal.h"
#include "CSManager.h"
#include "CSDotnetUtilties.h"
#include "Properties/CSPropertyGeneratorManager.h"
#include "Modules/ModuleManager.h"


#define LOCTEXT_NAMESPACE "FUnrealSharpCoreModule"

DEFINE_LOG_CATEGORY(LogUnrealSharp);

void FUnrealSharpCoreModule::StartupModule()
{
#if WITH_EDITOR
	if (!UnrealSharp::DotNetUtilities::VerifyCSharpEnvironment() || !UnrealSharp::DotNetUtilities::BuildUserSolution())
	{
		StartupModule();
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
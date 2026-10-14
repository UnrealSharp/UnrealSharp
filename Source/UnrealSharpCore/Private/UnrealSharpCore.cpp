#include "UnrealSharpCore.h"
#include "CoreMinimal.h"
#include "CSManager.h"
#include "CSDialogUtilities.h"
#include "CSDotnetUtilties.h"
#include "Properties/CSPropertyGeneratorManager.h"
#include "Modules/ModuleManager.h"

#if defined(__APPLE__)
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wpragma-once-outside-header"
#endif
#pragma once
#if defined(__APPLE__)
#pragma clang diagnostic pop
#endif

#define LOCTEXT_NAMESPACE "FUnrealSharpCoreModule"

DEFINE_LOG_CATEGORY(LogUnrealSharp);

void FUnrealSharpCoreModule::StartupModule()
{
#if WITH_EDITOR
	while (!UnrealSharp::DotNetUtilities::VerifyCSharpEnvironment() || !UnrealSharp::DotNetUtilities::BuildUserSolution())
	{
		if (!UnrealSharp::Dialogs::CanShowEditorUI())
		{
			UE_LOG(LogUnrealSharp, Error, TEXT("UnrealSharp initialization failed. No interactive dialog is available; exiting with status 1."));
			FPlatformMisc::RequestExitWithStatus(true, 1);
			return;
		}

		if (IsEngineExitRequested())
		{
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

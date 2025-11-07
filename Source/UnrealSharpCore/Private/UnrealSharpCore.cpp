#if defined(__APPLE__)
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wpragma-once-outside-header"
#endif
#pragma once
#if defined(__APPLE__)
#pragma clang diagnostic pop
#endif

#include "UnrealSharpCore.h"
#include "CoreMinimal.h"
#include "CSManager.h"
#include "Properties/CSPropertyGeneratorManager.h"
#include "CSProcHelper.h"
#include "UnrealSharpUtils.h"
#include "Modules/ModuleManager.h"

#define LOCTEXT_NAMESPACE "FUnrealSharpCoreModule"

DEFINE_LOG_CATEGORY(LogUnrealSharp);

void FUnrealSharpCoreModule::StartupModule()
{
#if WITH_EDITOR
	FString DotNetInstallationPath = FCSProcHelper::GetDotNetDirectory();
	if (DotNetInstallationPath.IsEmpty())
	{
		FString DialogText = FString::Printf(TEXT("UnrealSharp can't be initialized. An installation of .NET %s SDK can't be found on your system."), TEXT(DOTNET_MAJOR_VERSION));
		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(DialogText));
		return;
	}

	FString UnrealSharpLibraryPath = FCSProcHelper::GetUnrealSharpPluginsPath();
	if (!FPaths::FileExists(UnrealSharpLibraryPath))
	{
		FString FullPath = FPaths::ConvertRelativePathToFull(UnrealSharpLibraryPath);
		FString DialogText = FString::Printf(TEXT(
			"The bindings library could not be found at the following location:\n%s\n\n"
			"Most likely, the bindings library failed to build due to invalid generated glue."
		), *FullPath);

		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(DialogText));
		return;
	}

	TArray<FString> ProjectPaths;
	FCSProcHelper::GetAllProjectPaths(ProjectPaths, true);

	// Compile the C# project for any changes done outside the editor.
	if (!ProjectPaths.IsEmpty() && !FCSUnrealSharpUtils::IsStandalonePIE() && !FApp::IsUnattended() && !FCSProcHelper::InvokeUnrealSharpBuildTool(BUILD_ACTION_BUILD_EMIT_LOAD_ORDER))
	{
		StartupModule();
		return;
	}
#endif
	
	UCSManager& CSManager = UCSManager::GetOrCreate();
	CSManager.Initialize();
}

void FUnrealSharpCoreModule::ShutdownModule()
{
	UCSManager::Shutdown();
	FCSPropertyGeneratorManager::Shutdown();
}

#undef LOCTEXT_NAMESPACE
	
IMPLEMENT_MODULE(FUnrealSharpCoreModule, UnrealSharpCore)
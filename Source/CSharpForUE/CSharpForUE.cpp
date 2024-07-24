#if defined(__APPLE__)
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wpragma-once-outside-header"
#endif
#pragma once
#if defined(__APPLE__)
#pragma clang diagnostic pop
#endif

#include "CSharpForUE.h"
#include "CoreMinimal.h"
#include "CSManager.h"
#include "Modules/ModuleManager.h"

#define LOCTEXT_NAMESPACE "FCSharpForUEModule"

DEFINE_LOG_CATEGORY(LogUnrealSharp);

void FCSharpForUEModule::StartupModule()
{
	FCSManager::Get().InitializeUnrealSharp();
}

void FCSharpForUEModule::ShutdownModule()
{
	UE_LOG(LogUnrealSharp, Warning, TEXT("CSharpForUE module shutting down"));
}

#undef LOCTEXT_NAMESPACE
	
IMPLEMENT_MODULE(FCSharpForUEModule, CSharpForUE)
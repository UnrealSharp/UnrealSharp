#pragma once

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
}

#undef LOCTEXT_NAMESPACE
	
IMPLEMENT_MODULE(FCSharpForUEModule, CSharpForUE)
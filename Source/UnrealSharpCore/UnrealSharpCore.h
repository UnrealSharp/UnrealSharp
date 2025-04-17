﻿#if defined(__APPLE__)
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wpragma-once-outside-header"
#endif
#pragma once
#if defined(__APPLE__)
#pragma clang diagnostic pop
#endif


//#pragma once

#include "CoreMinimal.h"
#include "Modules/ModuleManager.h"

#if ENGINE_MINOR_VERSION >= 4
#define CS_EInternalObjectFlags_AllFlags EInternalObjectFlags_AllFlags
#else
#define CS_EInternalObjectFlags_AllFlags EInternalObjectFlags::AllFlags
#endif

DECLARE_LOG_CATEGORY_EXTERN(LogUnrealSharp, Log, All);

class FUnrealSharpCoreModule : public IModuleInterface
{
public:
	/** IModuleInterface implementation */
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;
};

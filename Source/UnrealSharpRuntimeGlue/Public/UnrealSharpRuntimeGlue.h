#pragma once

#include "CoreMinimal.h"
#include "CSGlueGenerator.h"
#include "Modules/ModuleManager.h"

class UCSGlueGenerator;

DECLARE_MULTICAST_DELEGATE_TwoParams(FOnRuntimeGlueChanged, UCSGlueGenerator*, const FString&);

DECLARE_LOG_CATEGORY_EXTERN(LogUnrealSharpRuntimeGlue, Log, All);

class FUnrealSharpRuntimeGlueModule : public IModuleInterface
{
public:
    virtual void StartupModule() override;
    virtual void ShutdownModule() override;

    UNREALSHARPRUNTIMEGLUE_API static FUnrealSharpRuntimeGlueModule& Get()
    {
        return FModuleManager::LoadModuleChecked<FUnrealSharpRuntimeGlueModule>("UnrealSharpRuntimeGlue");
    }
    UNREALSHARPRUNTIMEGLUE_API void ForceRefreshRuntimeGlue();
    UNREALSHARPRUNTIMEGLUE_API FOnRuntimeGlueChanged& GetOnRuntimeGlueChanged() { return OnRuntimeGlueChanged; }
    
private:
    void InitializeRuntimeGlueGenerators();
    void OnModulesChanged(FName ModuleName, EModuleChangeReason Reason);
    
    TMap<TObjectKey<UClass>, UCSGlueGenerator*> RuntimeGlueGenerators;
    FOnRuntimeGlueChanged OnRuntimeGlueChanged;
};

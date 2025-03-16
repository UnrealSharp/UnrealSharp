#include "UnrealSharpBinds.h"

#define LOCTEXT_NAMESPACE "FUnrealSharpBindsModule"

DEFINE_LOG_CATEGORY(LogUnrealSharpBinds);

void FUnrealSharpBindsModule::StartupModule()
{
}

void FUnrealSharpBindsModule::ShutdownModule()
{

}

#undef LOCTEXT_NAMESPACE
    
IMPLEMENT_MODULE(FUnrealSharpBindsModule, UnrealSharpBinds)
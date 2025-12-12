#include "UnrealSharpProcHelper.h"

#include "CSProcUtilities.h"

#define LOCTEXT_NAMESPACE "FUnrealSharpProcHelperModule"

DEFINE_LOG_CATEGORY(LogUnrealSharpProcHelper);

void FUnrealSharpProcHelperModule::StartupModule()
{

}

void FUnrealSharpProcHelperModule::ShutdownModule()
{
    
}

#undef LOCTEXT_NAMESPACE
    
IMPLEMENT_MODULE(FUnrealSharpProcHelperModule, UnrealSharpProcHelper)
#include "GlueGeneratorModule.h"
#include "CSGenerator.h"
#include "UnrealSharpProcHelper/CSProcHelper.h"

DEFINE_LOG_CATEGORY(LogGlueGenerator);

#define LOCTEXT_NAMESPACE "FGlueGeneratorModule"

void FGlueGeneratorModule::StartupModule()
{
	// Check if the C# API is up to date.
	FCSGenerator::Get().StartGenerator(FCSProcHelper::GetGeneratedClassesDirectory());
}

void FGlueGeneratorModule::ShutdownModule()
{
    
}

#undef LOCTEXT_NAMESPACE
    
IMPLEMENT_MODULE(FGlueGeneratorModule, GlueGenerator)
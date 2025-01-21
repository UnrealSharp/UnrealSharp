#include "UnrealSharpCompiler.h"
#include "CSBlueprintCompiler.h"
#include "CSCompilerContext.h"
#include "KismetCompiler.h"
#include "TypeGenerator/CSBlueprint.h"

#define LOCTEXT_NAMESPACE "FUnrealSharpCompilerModule"

void FUnrealSharpCompilerModule::StartupModule()
{
	FKismetCompilerContext::RegisterCompilerForBP(UCSBlueprint::StaticClass(), [](UBlueprint* InBlueprint, FCompilerResultsLog& InMessageLog, const FKismetCompilerOptions& InCompileOptions)
	{
		return MakeShared<FCSCompilerContext>(CastChecked<UCSBlueprint>(InBlueprint), InMessageLog, InCompileOptions);
	});
	
	IKismetCompilerInterface& KismetCompilerModule = FModuleManager::LoadModuleChecked<IKismetCompilerInterface>("KismetCompiler");
	KismetCompilerModule.GetCompilers().Add(&BlueprintCompiler);
}

void FUnrealSharpCompilerModule::ShutdownModule()
{
    
}

#undef LOCTEXT_NAMESPACE
    
IMPLEMENT_MODULE(FUnrealSharpCompilerModule, UnrealSharpCompiler)
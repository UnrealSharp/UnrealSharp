#include "CSRuntimeGlueSettings.h"

#include "UnrealSharpRuntimeGlue.h"
#include "DefaultGenerators/CSAssetManagerGlueGenerator.h"
#include "DefaultGenerators/CSGameplayTagsGlueGenerator.h"
#include "DefaultGenerators/CSStringTableGlueGenerator.h"
#include "DefaultGenerators/CSTraceTypeQueryGlueGenerator.h"

UCSRuntimeGlueSettings::UCSRuntimeGlueSettings()
{
	CategoryName = "Plugins";
	
	Generators.Add(UCSAssetManagerGlueGenerator::StaticClass());
	Generators.Add(UCSTraceTypeQueryGlueGenerator::StaticClass());
	Generators.Add(UCSGameplayTagsGlueGenerator::StaticClass());
	Generators.Add(UCSStringTableGlueGenerator::StaticClass());
}

void UCSRuntimeGlueSettings::PostEditChangeProperty(struct FPropertyChangedEvent& PropertyChangedEvent)
{
	Super::PostEditChangeProperty(PropertyChangedEvent);
	FUnrealSharpRuntimeGlueModule::Get().ForceRefreshRuntimeGlue();
}

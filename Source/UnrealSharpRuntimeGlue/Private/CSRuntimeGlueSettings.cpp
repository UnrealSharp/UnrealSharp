#include "CSRuntimeGlueSettings.h"
#include "DefaultGenerators/CSAssetManagerGlueGenerator.h"
#include "DefaultGenerators/CSGameplayTagsGlueGenerator.h"
#include "DefaultGenerators/CSTraceTypeQueryGlueGenerator.h"

UCSRuntimeGlueSettings::UCSRuntimeGlueSettings()
{
	CategoryName = "Plugins";
	
	Generators.Add(UCSAssetManagerGlueGenerator::StaticClass());
	Generators.Add(UCSTraceTypeQueryGlueGenerator::StaticClass());
	Generators.Add(UCSGameplayTagsGlueGenerator::StaticClass());
}

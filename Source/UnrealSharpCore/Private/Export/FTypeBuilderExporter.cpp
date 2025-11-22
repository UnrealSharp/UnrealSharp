#include "Export/FTypeBuilderExporter.h"
#include "CSManager.h"
#include "Factories/CSPropertyFactory.h"

void UFTypeBuilderExporter::RegisterManagedType_Native(char* InFieldName, char* InNamespace, char* InAssemblyName, char* JsonString, ECSFieldType FieldType, uint8* TypeHandle)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UFTypeBuilderExporter::NewType_Internal);
	
	UCSManagedAssembly* Assembly = UCSManager::Get().FindOrLoadAssembly(InAssemblyName);

	if (!IsValid(Assembly))
	{
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to find or load assembly: {0}", InAssemblyName);
		return;
	}
	
	Assembly->RegisterManagedType(InFieldName, InNamespace, FieldType, TypeHandle, JsonString);
}

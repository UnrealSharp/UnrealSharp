#include "Export/FTypeBuilderExporter.h"
#include "CSManager.h"
#include "Factories/CSPropertyFactory.h"

void UFTypeBuilderExporter::RegisterManagedType_Native(char* InFieldName, char* InNamespace, char* InAssemblyName, char* NewJsonReflectionData, ECSFieldType FieldType, uint8* TypeHandle)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UFTypeBuilderExporter::RegisterManagedType_Native);
	
	UCSManagedAssembly* Assembly = UCSManager::Get().FindAssembly(InAssemblyName);

	if (!IsValid(Assembly))
	{
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to find or load assembly: {0}", InAssemblyName);
	}
	
	Assembly->RegisterManagedType(InFieldName, InNamespace, FieldType, TypeHandle, NewJsonReflectionData);
}

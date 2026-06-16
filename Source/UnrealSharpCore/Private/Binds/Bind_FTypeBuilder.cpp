#include "CSManager.h"

DECLARE_UNREALSHARP_BINDER(Bind_FTypeBuilder)
{
	void RegisterManagedType_Native(TCHAR* InFieldName, TCHAR* InNamespace, TCHAR* InAssemblyName, TCHAR* NewJsonReflectionData, ECSFieldType FieldType, uint8* TypeHandle)
	{
		TRACE_CPUPROFILER_EVENT_SCOPE(UFTypeBuilderExporter::RegisterManagedType_Native);
		UCSManagedAssembly* Assembly = UCSManager::Get().FindAssembly(InAssemblyName);
		Assembly->RegisterManagedType(InFieldName, InNamespace, FieldType, TypeHandle, NewJsonReflectionData);
	}
	
	BIND_UNREALSHARP_FUNCTION(RegisterManagedType_Native)
}

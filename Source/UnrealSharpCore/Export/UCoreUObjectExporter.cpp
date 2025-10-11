#include "UCoreUObjectExporter.h"
#include "CSAssembly.h"
#include "CSManager.h"

UStruct* UUCoreUObjectExporter::GetType(const char* InAssemblyName, const char* InNamespace, const char* InTypeName)
{
	// This gets called by the static constructor of the type, so we can cache the type info of native classes here.
	UCSAssembly* Assembly = UCSManager::Get().FindOrLoadAssembly(InAssemblyName);
	FCSFieldName FieldName(InTypeName, InNamespace);
	TSharedPtr<FCSManagedTypeInfo> TypeInfo = Assembly->FindOrAddTypeInfo(FieldName);

	UStruct* Field = TypeInfo->GetFieldChecked<UStruct>();
	
	if (!IsValid(Field))
	{
		UE_LOGFMT(LogUnrealSharp, BreakOnLog, "Failed to find type: {0}.{1} in assembly {2}", InNamespace, InTypeName, InAssemblyName);
		return nullptr;
	}

	return Field;
}

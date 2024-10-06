#include "UCoreUObjectExporter.h"
#include "TypeGenerator/Register/CSTypeRegistry.h"

void UUCoreUObjectExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(GetNativeClassFromName)
	EXPORT_FUNCTION(GetNativeStructFromName)
}

UClass* UUCoreUObjectExporter::GetNativeClassFromName(const char* InClassName)
{
	UClass* Class = FCSTypeRegistry::GetClassFromName(InClassName);
	return Class;
}

UStruct* UUCoreUObjectExporter::GetNativeStructFromName(const char* InStructName)
{
	UStruct* Struct = FCSTypeRegistry::GetStructFromName(InStructName);
	return Struct;
}

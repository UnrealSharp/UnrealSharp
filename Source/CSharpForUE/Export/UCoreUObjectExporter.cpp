#include "UCoreUObjectExporter.h"
#include "CSharpForUE/TypeGenerator/Register/CSGeneratedClassBuilder.h"
#include "CSharpForUE/TypeGenerator/Register/CSGeneratedStructBuilder.h"

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
	return FCSTypeRegistry::GetStructFromName(InStructName);
}

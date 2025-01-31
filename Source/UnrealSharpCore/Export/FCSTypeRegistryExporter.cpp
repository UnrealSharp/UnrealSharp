#include "FCSTypeRegistryExporter.h"
#include "TypeGenerator/Register/CSTypeRegistry.h"

void UFCSTypeRegistryExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(RegisterClassToFilePath)
}

void UFCSTypeRegistryExporter::RegisterClassToFilePath(const UTF16CHAR* ClassName, const UTF16CHAR* FilePath)
{
	FCSTypeRegistry::Get().RegisterClassToFilePath(ClassName, FilePath);
}

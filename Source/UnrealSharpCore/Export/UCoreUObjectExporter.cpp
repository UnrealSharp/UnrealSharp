#include "UCoreUObjectExporter.h"
#include "CSAssembly.h"
#include "CSManager.h"
#include "TypeGenerator/Register/TypeInfo/CSClassInfo.h"

void UUCoreUObjectExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(GetNativeClassFromName)
	EXPORT_FUNCTION(GetNativeStructFromName)
}

UClass* UUCoreUObjectExporter::GetNativeClassFromName(const char* InAssemblyName, const char* InClassName)
{
	TSharedPtr<FCSAssembly> Assembly = UCSManager::Get().FindOrLoadAssembly(InAssemblyName);
	
	// This gets called by the static constructor of the class, so we can cache the class info of native classes here.
	TSharedPtr<FCSharpClassInfo> ClassInfo = Assembly->FindOrAddClassInfo(InClassName);
	
	return ClassInfo->Field;
}

UScriptStruct* UUCoreUObjectExporter::GetNativeStructFromName(const char* InAssemblyName, const char* InStructName)
{
	TSharedPtr<FCSAssembly> Assembly = UCSManager::Get().FindAssembly(InAssemblyName);
	UScriptStruct* ScriptStruct = Assembly->FindStruct(InStructName);
	return ScriptStruct;
}

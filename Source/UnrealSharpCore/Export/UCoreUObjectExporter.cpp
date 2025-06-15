#include "UCoreUObjectExporter.h"
#include "CSAssembly.h"
#include "CSManager.h"
#include "TypeGenerator/Register/TypeInfo/CSClassInfo.h"

UClass* UUCoreUObjectExporter::GetNativeClassFromName(const char* InAssemblyName, const char* InNamespace, const char* InClassName)
{
	// This gets called by the static constructor of the class, so we can cache the class info of native classes here.
	TSharedPtr<FCSAssembly> Assembly = UCSManager::Get().FindOrLoadAssembly(InAssemblyName);
	FCSFieldName FieldName(InClassName, InNamespace);
	
	TSharedPtr<FCSClassInfo> ClassInfo = Assembly->FindOrAddClassInfo(FieldName);
	return ClassInfo->Field;
}

UScriptStruct* UUCoreUObjectExporter::GetNativeStructFromName(const char* InAssemblyName, const char* InNamespace, const char* InStructName)
{
	TSharedPtr<FCSAssembly> Assembly = UCSManager::Get().FindOrLoadAssembly(InAssemblyName);
	FCSFieldName FieldName(InStructName, InNamespace);
	UScriptStruct* ScriptStruct = Assembly->FindStruct(FieldName);
	return ScriptStruct;
}

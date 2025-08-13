#include "UCoreUObjectExporter.h"
#include "CSAssembly.h"
#include "CSManager.h"
#include "TypeGenerator/Register/TypeInfo/CSClassInfo.h"

UClass* UUCoreUObjectExporter::GetNativeClassFromName(const char* InAssemblyName, const char* InNamespace, const char* InClassName)
{
	// This gets called by the static constructor of the class, so we can cache the class info of native classes here.
	UCSAssembly* Assembly = UCSManager::Get().FindOrLoadAssembly(InAssemblyName);
	FCSFieldName FieldName(InClassName, InNamespace);
	
	TSharedPtr<FCSClassInfo> ClassInfo = Assembly->FindOrAddClassInfo(FieldName);
	if (ClassInfo == nullptr)
	{
		return nullptr;
	}
	
	return ClassInfo->GetField<UClass>();
}

UClass* UUCoreUObjectExporter::GetNativeInterfaceFromName(const char* InAssemblyName, const char* InNamespace, const char* InInterfaceName)
{
	UCSAssembly* Assembly = UCSManager::Get().FindOrLoadAssembly(InAssemblyName);
	FCSFieldName FieldName(InInterfaceName, InNamespace);
	return Assembly->FindInterface(FieldName);
}

UScriptStruct* UUCoreUObjectExporter::GetNativeStructFromName(const char* InAssemblyName, const char* InNamespace, const char* InStructName)
{
	UCSAssembly* Assembly = UCSManager::Get().FindOrLoadAssembly(InAssemblyName);
	FCSFieldName FieldName(InStructName, InNamespace);
	UScriptStruct* ScriptStruct = Assembly->FindStruct(FieldName);
	return ScriptStruct;
}

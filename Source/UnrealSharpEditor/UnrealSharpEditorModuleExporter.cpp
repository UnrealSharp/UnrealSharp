#include "UnrealSharpEditorModuleExporter.h"
#include "CSManager.h"
#include "UnrealSharpProcHelper/CSProcHelper.h"

void UFUnrealSharpEditorModuleExporter::InitializeUnrealSharpEditorCallbacks(FCSManagedUnrealSharpEditorCallbacks Callbacks)
{
	FUnrealSharpEditorModule::Get().InitializeUnrealSharpEditorCallbacks(Callbacks);
}

void UFUnrealSharpEditorModuleExporter::GetProjectPaths(TArray<FString>* Paths)
{
	FCSProcHelper::GetAllProjectPaths(*Paths, true);
}

void UFUnrealSharpEditorModuleExporter::DirtyUnrealType(const char* AssemblyName, const char* Namespace, const char* TypeName)
{
	UCSAssembly* Assembly = UCSManager::Get().FindAssembly(UTF8_TO_TCHAR(AssemblyName));

	if (!IsValid(Assembly))
	{
		return;
	}

	FCSFieldName FieldName(UTF8_TO_TCHAR(TypeName), UTF8_TO_TCHAR(Namespace));
	TSharedPtr<FCSManagedTypeInfo> TypeInfo = Assembly->FindTypeInfo(FieldName);

	if (!TypeInfo.IsValid())
	{
		UE_LOGFMT(LogUnrealSharpEditor, Error, "Failed to dirty type {0}.{1} in assembly {2}: type not found", Namespace, TypeName, AssemblyName);
		return;
	}
	
	TypeInfo->MarkAsStructurallyModified();
}

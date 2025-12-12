#include "UnrealSharpEditorModuleExporter.h"
#include "CSManager.h"
#include "CSProcUtilities.h"

void UFUnrealSharpEditorModuleExporter::InitializeUnrealSharpEditorCallbacks(FCSManagedEditorCallbacks Callbacks)
{
	FUnrealSharpEditorModule::Get().InitializeManagedEditorCallbacks(Callbacks);
}

void UFUnrealSharpEditorModuleExporter::GetProjectPaths(TArray<FString>* Paths)
{
	UCSProcUtilities::GetAllProjectPaths(*Paths, true);
}

void UFUnrealSharpEditorModuleExporter::DirtyUnrealType(const char* AssemblyName, const char* Namespace, const char* TypeName)
{
	UCSManagedAssembly* Assembly = UCSManager::Get().FindAssembly(AssemblyName);

	if (!IsValid(Assembly))
	{
		return;
	}

	FCSFieldName FieldName(TypeName, Namespace);
	TSharedPtr<FCSManagedTypeDefinition> ManagedTypeDefinition = Assembly->FindManagedTypeDefinition(FieldName);

	if (!ManagedTypeDefinition.IsValid())
	{
		UE_LOGFMT(LogUnrealSharpEditor, Log, "Skipping dirty check: {0}.{1} isn't registered in assembly {2}. It may be a new managed type.", Namespace, TypeName, AssemblyName);
		return;
	}
	
	ManagedTypeDefinition->MarkStructurallyDirty();
}

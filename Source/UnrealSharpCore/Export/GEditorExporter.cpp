#include "GEditorExporter.h"

#if WITH_EDITOR
#include "Editor.h"
#include "EditorSubsystem.h"
#endif

void UGEditorExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(GetEditorSubsystem)
}

void* UGEditorExporter::GetEditorSubsystem(UClass* SubsystemClass)
{
#if WITH_EDITOR
	UEditorSubsystem* EditorSubsystem = GEditor->GetEditorSubsystemBase(SubsystemClass);
	return EditorSubsystem;
#else
	return nullptr;
#endif
}

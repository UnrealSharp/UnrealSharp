#include "GEditorExporter.h"
#include "Editor.h"
#include "EditorSubsystem.h"

void UGEditorExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(GetEditorSubsystem)
}

void* UGEditorExporter::GetEditorSubsystem(UClass* SubsystemClass)
{
	UEditorSubsystem* EditorSubsystem = GEditor->GetEditorSubsystemBase(SubsystemClass);
	return EditorSubsystem;
}

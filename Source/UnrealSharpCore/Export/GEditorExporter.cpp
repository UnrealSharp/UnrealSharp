#include "GEditorExporter.h"

#include "CSManager.h"

#if WITH_EDITOR
#include "Editor.h"
#include "EditorSubsystem.h"
#endif

void* UGEditorExporter::GetEditorSubsystem(UClass* SubsystemClass)
{
#if WITH_EDITOR
	const UEditorSubsystem* EditorSubsystem = GEditor->GetEditorSubsystemBase(SubsystemClass);
	return UCSManager::Get().FindManagedObject(EditorSubsystem);
#else
	return nullptr;
#endif
}

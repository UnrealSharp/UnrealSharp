#include "CSManager.h"

#if WITH_EDITOR
#include "Editor.h"
#include "EditorSubsystem.h"
#endif

DECLARE_UNREALSHARP_BINDER(Bind_GEditor)
{
	void* GetEditorSubsystem(UClass* SubsystemClass)
	{
#if WITH_EDITOR
		const UEditorSubsystem* EditorSubsystem = GEditor->GetEditorSubsystemBase(SubsystemClass);
		return UCSManager::Get().FindManagedObject(EditorSubsystem);
#else
		return nullptr;
#endif
	}
	
	BIND_UNREALSHARP_FUNCTION(GetEditorSubsystem)
}

#include "CSSourceCodeNavigation.h"

#include "Framework/Notifications/NotificationManager.h"
#include "Misc/EngineVersionComparison.h"
#include "UnrealSharpEditor.h"
#include "Types/CSClass.h"
#include "Widgets/Notifications/SNotificationList.h"

#define LOCTEXT_NAMESPACE "CSSourceCodeNavigation"

namespace
{
	// Report failures like native source navigation does: a notification instead of a blocking dialog.
	void NotifyOpenClassFailed(const FText& Reason)
	{
		FNotificationInfo Info(FText::Format(LOCTEXT("OpenClassFailed", "Failed to open C# source: {0}"), Reason));
		Info.ExpireDuration = 5.0f;
		if (const TSharedPtr<SNotificationItem> Notification = FSlateNotificationManager::Get().AddNotification(Info))
		{
			Notification->SetCompletionState(SNotificationItem::CS_Fail);
		}
	}
}

bool FCSSourceCodeNavigation::CanNavigateToClass(const UClass* Class)
{
	const UCSClass* ManagedClass = Cast<UCSClass>(Class);
	return IsValid(ManagedClass) && ManagedClass->HasManagedTypeDefinition();
}

bool FCSSourceCodeNavigation::NavigateToClass(const UClass* Class)
{
	if (!CanNavigateToClass(Class))
	{
		return false;
	}

	OpenClass(Class);
	// The request was handled, including any error. Do not fall back to C++ header lookup.
	return true;
}

bool FCSSourceCodeNavigation::OpenClass(const UClass* Class)
{
	const UCSClass* ManagedClass = Cast<UCSClass>(Class);
	if (!IsValid(ManagedClass) || !ManagedClass->HasManagedTypeDefinition())
	{
		return false;
	}

	const FCSManagedEditorCallbacks& Callbacks = FUnrealSharpEditorModule::Get().GetManagedEditorCallbacks();
	if (!Callbacks.FindClassSource)
	{
		NotifyOpenClassFailed(LOCTEXT("EditorNotReady", "The C# editor is not initialized yet. Try again after the C# solution has loaded."));
		return false;
	}

	const FCSFieldName& FieldName = ManagedClass->GetReflectionData()->FieldName;
	FString FilePath;
	FString Error;
	int32 Line = 0;
	int32 Column = 0;
	if (!Callbacks.FindClassSource(*FieldName.GetAssemblyName().ToString(), *FieldName.GetFullName(), &FilePath, &Line, &Column, &Error))
	{
		NotifyOpenClassFailed(FText::FromString(Error));
		return false;
	}

	// Open the file the same way native class navigation does on this engine version.
#if UE_VERSION_OLDER_THAN(5, 8, 0)
	return FSourceCodeNavigation::OpenSourceFile(FilePath, Line, Column);
#else
	FSourceCodeNavigation::OpenSourceFileAsync(FilePath, Line, Column);
	return true;
#endif
}

#undef LOCTEXT_NAMESPACE

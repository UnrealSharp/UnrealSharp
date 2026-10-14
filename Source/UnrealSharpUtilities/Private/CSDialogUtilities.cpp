#include "CSDialogUtilities.h"

#include "UnrealSharpUtilities.h"
#include "Framework/Application/SlateApplication.h"
#include "Misc/App.h"
#include "Misc/MessageDialog.h"

bool UnrealSharp::Dialogs::CanShowEditorUI()
{
	return !FApp::IsUnattended() && !GIsRunningUnattendedScript && !IsRunningCommandlet()
		&& FApp::CanEverRender() && FSlateApplication::IsInitialized() && IsInGameThread();
}

void UnrealSharp::Dialogs::ShowError(const FText& Message)
{
	UE_LOG(LogUnrealSharpUtilities, Error, TEXT("%s"), *Message.ToString());
	if (CanShowEditorUI())
	{
		FMessageDialog::Open(EAppMsgType::Ok, Message);
	}
}

FCSCommandError UnrealSharp::Dialogs::MakeDialogOnError()
{
	return FCSCommandError::CreateLambda([](const FString& ErrorOutput)
	{
		// InvokeCommand has already logged the command output.
		if (CanShowEditorUI())
		{
			FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(ErrorOutput));
		}
	});
}

FCSCommandError UnrealSharp::Dialogs::MakeOkCancelDialogOnError()
{
	return FCSCommandError::CreateLambda([](const FString& ErrorOutput)
	{
		// Let the caller handle failure without a dialog or a successful exit code.
		if (!CanShowEditorUI())
		{
			return;
		}

		EAppReturnType::Type Result = FMessageDialog::Open(EAppMsgType::OkCancel, FText::FromString(ErrorOutput));
		if (Result == EAppReturnType::Cancel)
		{
			FPlatformMisc::RequestExit(true);
		}
	});
}

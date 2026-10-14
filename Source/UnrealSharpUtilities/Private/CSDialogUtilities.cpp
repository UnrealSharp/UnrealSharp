#include "CSDialogUtilities.h"

#include "Framework/Application/SlateApplication.h"
#include "Misc/App.h"

bool UnrealSharp::Dialogs::CanShowEditorUI()
{
	return !FApp::IsUnattended() && !GIsRunningUnattendedScript && !IsRunningCommandlet()
		&& FApp::CanEverRender() && FSlateApplication::IsInitialized() && IsInGameThread();
}

FCSCommandError UnrealSharp::Dialogs::MakeDialogOnError()
{
	return FCSCommandError::CreateLambda([](const FString& ErrorOutput)
	{
		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(ErrorOutput));
	});
}

FCSCommandError UnrealSharp::Dialogs::MakeOkCancelDialogOnError()
{
	return FCSCommandError::CreateLambda([](const FString& ErrorOutput)
{
	EAppReturnType::Type Result = FMessageDialog::Open(EAppMsgType::OkCancel, FText::FromString(ErrorOutput));
		
	if (Result == EAppReturnType::Cancel)
	{
		FPlatformMisc::RequestExit(true);
	}
});
}

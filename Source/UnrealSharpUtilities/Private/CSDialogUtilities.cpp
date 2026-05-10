#include "CSDialogUtilities.h"

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

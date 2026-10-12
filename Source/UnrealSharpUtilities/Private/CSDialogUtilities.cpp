#include "CSDialogUtilities.h"

#include "UnrealSharpUtilities.h"
#include "Framework/Application/SlateApplication.h"
#include "Framework/Notifications/NotificationManager.h"
#include "Logging/StructuredLog.h"
#include "Misc/App.h"
#include "Widgets/Notifications/SNotificationList.h"

namespace
{
	void ShowHeadlessNotification(const FText& Message, const FText& Title)
	{
		if (!FSlateApplication::IsInitialized() || !IsInGameThread())
		{
			return;
		}

		FNotificationInfo Info(Title.IsEmpty() ? Message : Title);
		Info.SubText = Title.IsEmpty() ? FText::GetEmpty() : Message;
		Info.ExpireDuration = 10.0f;
		Info.bFireAndForget = true;
		FSlateNotificationManager::Get().AddNotification(Info);
	}
}

bool UnrealSharp::Dialogs::IsHeadless()
{
	return FApp::IsUnattended() || GIsRunningUnattendedScript || IsRunningCommandlet() || !FApp::CanEverRender();
}

EAppReturnType::Type UnrealSharp::Dialogs::OpenMessageDialog(EAppMsgType::Type MessageType, EAppReturnType::Type DefaultResult, const FText& Message, const FText& Title)
{
	if (!IsHeadless())
	{
		return Title.IsEmpty()
			? FMessageDialog::Open(MessageType, DefaultResult, Message)
			: FMessageDialog::Open(MessageType, DefaultResult, Message, Title);
	}

	UE_LOGFMT(LogUnrealSharpUtilities, Display, "Running headless, not showing dialog \"{0}\" ({1}). Using the default answer {2}.",
		Title.ToString(), Message.ToString(), static_cast<int32>(DefaultResult));
	ShowHeadlessNotification(Message, Title);
	return DefaultResult;
}

namespace
{
	void ShowOkDialogOrNotification(const FText& Message, const FText& Title)
	{
		if (UnrealSharp::Dialogs::IsHeadless())
		{
			ShowHeadlessNotification(Message, Title);
		}
		else if (Title.IsEmpty())
		{
			FMessageDialog::Open(EAppMsgType::Ok, Message);
		}
		else
		{
			FMessageDialog::Open(EAppMsgType::Ok, Message, Title);
		}
	}

	FString FormatForLog(const FText& Message, const FText& Title)
	{
		return Title.IsEmpty() ? Message.ToString() : Title.ToString() + TEXT(": ") + Message.ToString();
	}
}

void UnrealSharp::Dialogs::ShowError(const FText& Message, const FText& Title)
{
	UE_LOGFMT(LogUnrealSharpUtilities, Error, "{0}", FormatForLog(Message, Title));
	ShowOkDialogOrNotification(Message, Title);
}

void UnrealSharp::Dialogs::ShowWarning(const FText& Message, const FText& Title)
{
	UE_LOGFMT(LogUnrealSharpUtilities, Warning, "{0}", FormatForLog(Message, Title));
	ShowOkDialogOrNotification(Message, Title);
}

FCSCommandError UnrealSharp::Dialogs::MakeDialogOnError()
{
	return FCSCommandError::CreateLambda([](const FString& ErrorOutput)
	{
		ShowOkDialogOrNotification(FText::FromString(ErrorOutput), FText::GetEmpty());
	});
}

FCSCommandError UnrealSharp::Dialogs::MakeOkCancelDialogOnError()
{
	return FCSCommandError::CreateLambda([](const FString& ErrorOutput)
	{
		if (IsHeadless())
		{
			// RequestExit(true) exits with 0 on Windows; let StartupModule fail instead.
			UE_LOGFMT(LogUnrealSharpUtilities, Display, "Running headless, not showing the OK/Cancel dialog for this error.");
			return;
		}

		EAppReturnType::Type Result = FMessageDialog::Open(EAppMsgType::OkCancel, FText::FromString(ErrorOutput));

		if (Result == EAppReturnType::Cancel)
		{
			FPlatformMisc::RequestExit(true);
		}
	});
}

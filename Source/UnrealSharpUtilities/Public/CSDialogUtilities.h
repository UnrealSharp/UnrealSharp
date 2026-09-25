#pragma once
#include "CSProcessUtilities.h"
#include "Misc/MessageDialog.h"

namespace UnrealSharp::Dialogs
{
	// True when nobody can answer a modal dialog: -unattended, a commandlet, an unattended script,
	// or an editor started with -nullrhi. A modal dialog would block such a process forever.
	UNREALSHARPUTILITIES_API bool IsHeadless();

	// Logs the message and opens a modal dialog. When headless, shows a non-blocking notification
	// instead and returns DefaultResult.
	UNREALSHARPUTILITIES_API EAppReturnType::Type OpenMessageDialog(EAppMsgType::Type MessageType, EAppReturnType::Type DefaultResult, const FText& Message, const FText& Title = FText::GetEmpty());

	// Logs the error and reports it with a modal dialog, or a notification when headless.
	UNREALSHARPUTILITIES_API void ShowError(const FText& Message, const FText& Title = FText::GetEmpty());

	UNREALSHARPUTILITIES_API FCSCommandError MakeDialogOnError();
	UNREALSHARPUTILITIES_API FCSCommandError MakeOkCancelDialogOnError();
}

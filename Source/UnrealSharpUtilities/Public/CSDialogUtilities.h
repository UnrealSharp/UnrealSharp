#pragma once
#include "CSProcessUtilities.h"
#include "Misc/MessageDialog.h"

namespace UnrealSharp::Dialogs
{
	/**
	 * Whether nobody can answer a modal dialog: the process runs with -unattended, as a commandlet, an unattended
	 * script, or an editor started with -nullrhi. A modal dialog would block such a process forever.
	 */
	UNREALSHARPUTILITIES_API bool IsHeadless();

	/**
	 * Opens a modal message dialog. When headless (see IsHeadless), logs the message, shows a non-blocking
	 * notification if Slate is running, and returns DefaultResult without blocking.
	 */
	UNREALSHARPUTILITIES_API EAppReturnType::Type OpenMessageDialog(EAppMsgType::Type MessageType, EAppReturnType::Type DefaultResult, const FText& Message, const FText& Title = FText::GetEmpty());

	/**
	 * Logs the error and reports it with a modal dialog, or with a non-blocking notification when headless.
	 */
	UNREALSHARPUTILITIES_API void ShowError(const FText& Message, const FText& Title = FText::GetEmpty());

	UNREALSHARPUTILITIES_API FCSCommandError MakeDialogOnError();
	UNREALSHARPUTILITIES_API FCSCommandError MakeOkCancelDialogOnError();
}

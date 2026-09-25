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

	/**
	 * Logs the message as a warning and reports it with a modal dialog, or with a non-blocking notification when
	 * headless. Use this for problems that should not fail an automation run.
	 */
	UNREALSHARPUTILITIES_API void ShowWarning(const FText& Message, const FText& Title = FText::GetEmpty());

	/**
	 * Creates a command error callback that reports the error with a modal dialog, or with a non-blocking
	 * notification when headless. The command has already logged the error, so it is not logged again.
	 */

	UNREALSHARPUTILITIES_API FCSCommandError MakeDialogOnError();
	UNREALSHARPUTILITIES_API FCSCommandError MakeOkCancelDialogOnError();
}

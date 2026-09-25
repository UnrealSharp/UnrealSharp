#pragma once
#include "CSProcessUtilities.h"
#include "Misc/MessageDialog.h"

namespace UnrealSharp::Dialogs
{
	// -unattended, commandlet, unattended script or -nullrhi: nobody can answer a modal dialog.
	UNREALSHARPUTILITIES_API bool IsHeadless();

	// Headless: logs, shows a notification and returns DefaultResult instead of blocking.
	UNREALSHARPUTILITIES_API EAppReturnType::Type OpenMessageDialog(EAppMsgType::Type MessageType, EAppReturnType::Type DefaultResult, const FText& Message, const FText& Title = FText::GetEmpty());

	UNREALSHARPUTILITIES_API void ShowError(const FText& Message, const FText& Title = FText::GetEmpty());

	UNREALSHARPUTILITIES_API FCSCommandError MakeDialogOnError();
	UNREALSHARPUTILITIES_API FCSCommandError MakeOkCancelDialogOnError();
}

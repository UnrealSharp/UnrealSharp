#pragma once
#include "CSProcessUtilities.h"

namespace UnrealSharp::Dialogs
{
	/** Whether editor UI can be shown interactively on the current thread. */
	UNREALSHARPUTILITIES_API bool CanShowEditorUI();

	UNREALSHARPUTILITIES_API FCSCommandError MakeDialogOnError();
	UNREALSHARPUTILITIES_API FCSCommandError MakeOkCancelDialogOnError();
}

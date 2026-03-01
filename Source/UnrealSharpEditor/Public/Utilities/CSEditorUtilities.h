#pragma once

namespace FCSEditorUtilities
{
	TSharedPtr<SNotificationItem> MakeNotification(const FSlateIcon& Icon, const FString& Text);
	UNREALSHARPEDITOR_API FString ReplaceSpecialCharacters(const FString& Input);
};

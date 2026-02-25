#include "Utilities/CSEditorUtilities.h"

#include "Framework/Notifications/NotificationManager.h"
#include "Widgets/Notifications/SNotificationList.h"

TSharedPtr<SNotificationItem> FCSEditorUtilities::MakeNotification(const FSlateIcon& Icon, const FString& Text)
{
	FNotificationInfo Info(FText::FromString(Text));
	Info.Image = Icon.GetIcon();
	Info.bFireAndForget = false;
	Info.FadeOutDuration = 0.0f;
	Info.ExpireDuration = 0.0f;

	TSharedPtr<SNotificationItem> NewNotification = FSlateNotificationManager::Get().AddNotification(Info);
	NewNotification->SetCompletionState(SNotificationItem::CS_Pending);
	return NewNotification;
}

FString FCSEditorUtilities::ReplaceSpecialCharacters(const FString& Input)
{
	FString ModifiedString = Input;
	FRegexPattern Pattern(TEXT("[^a-zA-Z0-9_]"));
	FRegexMatcher Matcher(Pattern, ModifiedString);

	while (Matcher.FindNext())
	{
		int32 MatchStart = Matcher.GetMatchBeginning();
		int32 MatchEnd = Matcher.GetMatchEnding();
		ModifiedString = ModifiedString.Mid(0, MatchStart) + TEXT("_") + ModifiedString.Mid(MatchEnd);
		Matcher = FRegexMatcher(Pattern, ModifiedString);
	}

	return ModifiedString;
}

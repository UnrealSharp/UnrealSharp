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

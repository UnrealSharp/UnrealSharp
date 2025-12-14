#include "CSUnrealSharpSettings.h"

UCSUnrealSharpSettings::UCSUnrealSharpSettings()
{
	CategoryName = "Plugins";
}

#if WITH_EDITOR
void UCSUnrealSharpSettings::PreEditChange(FProperty* PropertyAboutToChange)
{
	Super::PreEditChange(PropertyAboutToChange);

	if (PropertyAboutToChange->GetFName() == GET_MEMBER_NAME_CHECKED(UCSUnrealSharpSettings, bEnableNamespaceSupport))
	{
		OldValueOfNamespaceSupport = bEnableNamespaceSupport;
	}
}

void UCSUnrealSharpSettings::PostEditChangeProperty(FPropertyChangedEvent& PropertyChangedEvent)
{
	Super::PostEditChangeProperty(PropertyChangedEvent);

	if (PropertyChangedEvent.Property)
	{
		const FName PropertyName = PropertyChangedEvent.Property->GetFName();
		if (PropertyName == GET_MEMBER_NAME_CHECKED(UCSUnrealSharpSettings, bEnableNamespaceSupport))
		{
			bRecentlyChangedNamespaceSupport = true;

			FText Message = FText::FromString(
				TEXT("Namespace support settings have been updated. A restart is required for the changes to take effect.\n\n"
					 "WARNING: This experimental feature will break existing Blueprints derived from C# classes due to changes in the outermost package when restarting the engine.\n\n"
					 "Press 'Cancel' to revert these changes.")
			);
			
			if (FMessageDialog::Open(EAppMsgType::OkCancel, Message) == EAppReturnType::Cancel)
			{
				bEnableNamespaceSupport = OldValueOfNamespaceSupport;
				bRecentlyChangedNamespaceSupport = false;
			}
		}
	}
}
#endif

bool UCSUnrealSharpSettings::HasNamespaceSupport() const
{
	if (bRecentlyChangedNamespaceSupport)
	{
		// Keep returning the old value until we have restarted the editor
		return OldValueOfNamespaceSupport;
	}
	
	return bEnableNamespaceSupport;
}

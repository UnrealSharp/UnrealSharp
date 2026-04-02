#pragma once

#include "CoreMinimal.h"
#include "Engine/DeveloperSettings.h"
#include "CSDeveloperSettings.generated.h"

UCLASS(Abstract)
class UCSDeveloperSettings : public UDeveloperSettings
{
	GENERATED_BODY()
#if WITH_EDITOR
	// UDeveloperSettings interface
	virtual bool SupportsAutoRegistration() const override { return false; }
	// End of UDeveloperSettings interface
#endif
};

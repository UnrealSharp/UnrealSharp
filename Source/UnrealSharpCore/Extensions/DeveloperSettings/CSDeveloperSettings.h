#pragma once

#include "CoreMinimal.h"
#include "Engine/DeveloperSettings.h"
#include "CSDeveloperSettings.generated.h"

UCLASS(Abstract, meta = (NotGeneratorValid))
class UCSDeveloperSettings : public UDeveloperSettings
{
	GENERATED_BODY()

	// UDeveloperSettings interface
	virtual bool SupportsAutoRegistration() const override { return false; }
	// End of UDeveloperSettings interface
};

#pragma once

#include "CoreMinimal.h"
#include "Engine/DeveloperSettings.h"
#include "CSUnrealSharpSettings.generated.h"

UCLASS(config = UnrealSharp, defaultconfig, meta = (DisplayName = "UnrealSharp Settings"))
class UNREALSHARPCORE_API UCSUnrealSharpSettings : public UDeveloperSettings
{
	GENERATED_BODY()
	
public:

	UCSUnrealSharpSettings();

#if WITH_EDITOR
	// UObject interface
	virtual void PreEditChange(FProperty* PropertyAboutToChange) override;
	virtual void PostEditChangeProperty(FPropertyChangedEvent& PropertyChangedEvent) override;
	// End of UObject interface
#endif
	
	// Should we exit PIE when an exception is thrown in C#?
	UPROPERTY(EditDefaultsOnly, config, Category = "UnrealSharp | Debugging")
	bool bCrashOnException = true;

	bool HasNamespaceSupport() const;

protected:
	
	// Should we enable namespace support for generated types?
	// If false, all types will be generated in the global package and all types need to have unique names.
	// Currently destructive to the project if changed after BPs of C# types have been created.
	UPROPERTY(EditDefaultsOnly, config, Category = "UnrealSharp | Namespace", Experimental)
	bool bEnableNamespaceSupport = false;

	bool bRecentlyChangedNamespaceSupport = false;
	bool OldValueOfNamespaceSupport = false;
};

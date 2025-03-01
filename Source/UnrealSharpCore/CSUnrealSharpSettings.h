#pragma once

#include "CoreMinimal.h"
#include "Engine/DeveloperSettings.h"
#include "CSUnrealSharpSettings.generated.h"

UCLASS(config = UnrealSharp, defaultconfig, meta = (DisplayName = "UnrealSharp Settings"))
class UNREALSHARPCORE_API UCSUnrealSharpSettings : public UDeveloperSettings
{
	GENERATED_BODY()
public:
	// Should we exit PIE when an exception is thrown in C#?
	UPROPERTY(EditDefaultsOnly, config, Category = "UnrealSharp | Debugging")
	bool bCrashOnException = true;
	
	// Should we enable namespace support for generated types?
	// If false, all types will be generated in the global package and all types need to have unique names.
	UPROPERTY(EditDefaultsOnly, config, Category = "UnrealSharp | Namespace")
	bool bEnableNamespaceSupport = false;
};

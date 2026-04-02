#pragma once

#include "CoreMinimal.h"
#include "GameFramework/CheatManager.h"
#include "CSCheatManagerExtension.generated.h"

UCLASS(Abstract, MinimalAPI)
class UCSCheatManagerExtension : public UCheatManagerExtension
{
	GENERATED_BODY()
public:
	UCSCheatManagerExtension();
};

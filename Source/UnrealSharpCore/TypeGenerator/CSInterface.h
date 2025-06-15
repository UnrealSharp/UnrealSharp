#pragma once

#include "CoreMinimal.h"
#include "CSInterface.generated.h"

struct FCSInterfaceInfo;

UCLASS(MinimalAPI)
class UCSInterface : public UClass
{
	GENERATED_BODY()
public:
	void SetInterfaceInfo(const TSharedPtr<FCSInterfaceInfo>& InInterfaceInfo);
	TSharedPtr<FCSInterfaceInfo> GetInterfaceInfo() const { return InterfaceInfo; }
private:
	TSharedPtr<FCSInterfaceInfo> InterfaceInfo;
};

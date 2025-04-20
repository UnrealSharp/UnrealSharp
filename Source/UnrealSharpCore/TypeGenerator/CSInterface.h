#pragma once

#include "CoreMinimal.h"
#include "CSInterface.generated.h"

struct FCSharpInterfaceInfo;

UCLASS(MinimalAPI)
class UCSInterface : public UClass
{
	GENERATED_BODY()
public:
	void SetInterfaceInfo(const TSharedPtr<FCSharpInterfaceInfo>& InInterfaceInfo);
	TSharedPtr<FCSharpInterfaceInfo> GetInterfaceInfo() const { return InterfaceInfo; }
private:
	TSharedPtr<FCSharpInterfaceInfo> InterfaceInfo;
};

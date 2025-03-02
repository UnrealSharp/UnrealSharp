#pragma once

#include "CoreMinimal.h"
#include "CSInterface.generated.h"

struct FCSharpInterfaceInfo;

UCLASS()
class UCSInterface : public UClass
{
	GENERATED_BODY()
public:
	void SetInterfaceInfo(const TSharedPtr<FCSharpInterfaceInfo>& InInterfaceInfo);
	UNREALSHARPCORE_API TSharedPtr<FCSharpInterfaceInfo> GetInterfaceInfo() const { return InterfaceInfo; }
private:
	TSharedPtr<FCSharpInterfaceInfo> InterfaceInfo;
};

#pragma once

#include "CoreMinimal.h"
#include "CSClass.h"
#include "CSSkeletonClass.generated.h"

class UCSClass;

UCLASS()
class UNREALSHARPCORE_API UCSSkeletonClass : public UCSClass
{
	GENERATED_BODY()
public:
	void SetGeneratedClass(UCSClass* InGeneratedClass) { GeneratedClass = InGeneratedClass; }
	UCSClass* GetGeneratedClass() const { return GeneratedClass; }
private:
	UPROPERTY()
	TObjectPtr<UCSClass> GeneratedClass;
};

#pragma once

#include "CoreMinimal.h"
#include "CSClass.h"
#include "CSSkeletonClass.generated.h"

class UCSClass;

UCLASS()
class UCSSkeletonClass : public UCSClass
{
	GENERATED_BODY()
public:
	UNREALSHARPCORE_API void SetGeneratedClass(UCSClass* InGeneratedClass) { GeneratedClass = InGeneratedClass; }
	UNREALSHARPCORE_API UCSClass* GetGeneratedClass() const { return GeneratedClass; }
private:
	UPROPERTY()
	TObjectPtr<UCSClass> GeneratedClass;
};

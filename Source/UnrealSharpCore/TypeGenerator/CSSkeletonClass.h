#pragma once

#include "CoreMinimal.h"
#include "Animation/AnimBlueprintGeneratedClass.h"
#include "CSSkeletonClass.generated.h"

class UCSClass;

UCLASS(MinimalAPI)
class UCSSkeletonClass : public UBlueprintGeneratedClass
{
	GENERATED_BODY()
public:
	UNREALSHARPCORE_API void SetGeneratedClass(UCSClass* InGeneratedClass);
	UNREALSHARPCORE_API UCSClass* GetGeneratedClass() const { return GeneratedClass; }
private:
	UPROPERTY()
	TObjectPtr<UCSClass> GeneratedClass;
};

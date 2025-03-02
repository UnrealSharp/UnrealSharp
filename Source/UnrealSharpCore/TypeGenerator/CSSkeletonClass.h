#pragma once

#include "CoreMinimal.h"
#include "Animation/AnimBlueprintGeneratedClass.h"
#include "CSSkeletonClass.generated.h"

class UCSClass;

UCLASS()
class UNREALSHARPCORE_API UCSSkeletonClass : public UBlueprintGeneratedClass
{
	GENERATED_BODY()
public:
	void SetGeneratedClass(UCSClass* InGeneratedClass);
	UCSClass* GetGeneratedClass() const { return GeneratedClass; }
private:
	UPROPERTY()
	TObjectPtr<UCSClass> GeneratedClass;
};

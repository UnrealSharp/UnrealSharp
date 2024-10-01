#pragma once

#include "CoreMinimal.h"
#include "UObject/Package.h"
#include "Engine/BlueprintGeneratedClass.h"
#include "CSClass.generated.h"

class FCSGeneratedClassBuilder;
class UCSFunctionBase;
struct FCSharpClassInfo;

UCLASS()
class CSHARPFORUE_API UCSClass : public UBlueprintGeneratedClass
{
	GENERATED_BODY()
public:
	friend FCSGeneratedClassBuilder;

	TSharedRef<FCSharpClassInfo> GetClassInfo() const;

private:

	bool bCanTick = true;
	TSharedPtr<FCSharpClassInfo> ClassMetaData;
	
};
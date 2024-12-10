#pragma once

#include "CoreMinimal.h"
#include "UObject/Package.h"
#include "Engine/BlueprintGeneratedClass.h"
#include "CSClass.generated.h"

class FCSGeneratedClassBuilder;
class UCSFunctionBase;
struct FCSharpClassInfo;

UCLASS()
class UNREALSHARPCORE_API UCSClass : public UBlueprintGeneratedClass
{
	GENERATED_BODY()
public:

	TSharedRef<const FCSharpClassInfo> GetClassInfo() const;
	bool CanTick() const { return bCanTick; }
	void SetClassMetaData(const TSharedPtr<FCSharpClassInfo>& InClassMetaData);

private:

	bool bCanTick = true;
	TSharedPtr<FCSharpClassInfo> ClassMetaData;
	
};
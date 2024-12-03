#pragma once

#include "CoreMinimal.h"
#include "CSCommonPropertyGenerator.h"
#include "CSPropertyGenerator.h"
#include "CSObjectPropertyGenerator.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSObjectPropertyGenerator : public UCSCommonPropertyGenerator
{
	GENERATED_BODY()
	
public:

	UCSObjectPropertyGenerator(FObjectInitializer const& ObjectInitializer);

	// Begin UCSPropertyGenerator interface
	virtual auto CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) -> FProperty* override;
	// End UCSPropertyGenerator interface
};

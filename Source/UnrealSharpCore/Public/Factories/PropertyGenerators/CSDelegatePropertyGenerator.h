#pragma once

#include "CoreMinimal.h"
#include "CSCommonPropertyGenerator.h"
#include "CSPropertyGenerator.h"
#include "CSDelegatePropertyGenerator.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSDelegatePropertyGenerator : public UCSCommonPropertyGenerator
{
	GENERATED_BODY()
public:
	UCSDelegatePropertyGenerator(FObjectInitializer const& ObjectInitializer);
protected:
	// UCSPropertyGenerator interface
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	// End of UCSPropertyGenerator interface
};

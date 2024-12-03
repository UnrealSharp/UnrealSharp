#pragma once

#include "CoreMinimal.h"
#include "CSPropertyGenerator.h"
#include "CSCommonPropertyGenerator.generated.h"

UCLASS(Abstract)
class UNREALSHARPCORE_API UCSCommonPropertyGenerator : public UCSPropertyGenerator
{
	GENERATED_BODY()
protected:
	// Begin UCSPropertyGenerator interface
	virtual bool SupportsPropertyType(ECSPropertyType InPropertyType) const override;
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	// End UCSPropertyGenerator interface
	
	TMap<ECSPropertyType, FFieldClass*> TypeToFieldClass;
};

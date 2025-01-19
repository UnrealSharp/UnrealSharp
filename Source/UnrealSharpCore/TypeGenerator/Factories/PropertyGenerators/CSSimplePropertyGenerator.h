#pragma once

#include "CoreMinimal.h"
#include "CSCommonPropertyGenerator.h"
#include "CSSimplePropertyGenerator.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSSimplePropertyGenerator : public UCSCommonPropertyGenerator
{
	GENERATED_BODY()

public:

	UCSSimplePropertyGenerator(FObjectInitializer const& ObjectInitializer);

protected:

	// Begin UCSPropertyGenerator interface
	virtual bool SupportsPropertyType(ECSPropertyType InPropertyType) const override;
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::Unknown; }
	virtual FFieldClass* GetPropertyClass() override;
#if WITH_EDITOR
	virtual FEdGraphPinType GetPinType(ECSPropertyType PropertyType, const FCSPropertyMetaData& MetaData, UBlueprint* Outer) const override;
#endif
	// End UCSPropertyGenerator interface
};

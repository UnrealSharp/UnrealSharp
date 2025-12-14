#pragma once

#include "CoreMinimal.h"
#include "CSPropertyGenerator.h"
#include "CSDefaultComponentPropertyGenerator.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSDefaultComponentPropertyGenerator : public UCSPropertyGenerator
{
	GENERATED_BODY()
public:
	// UCSPropertyGenerator interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::DefaultComponent; }
	virtual TSharedPtr<FCSUnrealType> CreatePropertyInnerTypeData(ECSPropertyType PropertyType) override;
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData) override;
	// End of implementation
};

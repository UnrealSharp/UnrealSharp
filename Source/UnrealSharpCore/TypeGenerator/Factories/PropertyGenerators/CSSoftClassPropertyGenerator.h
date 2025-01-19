#pragma once

#include "CoreMinimal.h"
#include "CSPropertyGenerator.h"
#include "CSSoftClassPropertyGenerator.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSSoftClassPropertyGenerator : public UCSPropertyGenerator
{
	GENERATED_BODY()

protected:

	// Begin UCSPropertyGenerator interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::SoftClass; }
	virtual FFieldClass* GetPropertyClass() override { return FSoftClassProperty::StaticClass(); }
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	virtual TSharedPtr<FCSUnrealType> CreateTypeMetaData(ECSPropertyType PropertyType) override;
#if WITH_EDITOR
	virtual FEdGraphPinType GetPinType(ECSPropertyType PropertyType, const FCSPropertyMetaData& MetaData, UBlueprint* Outer) const override;
#endif
	// End UCSPropertyGenerator interface
};

#pragma once

#include "CoreMinimal.h"
#include "CSPropertyGenerator.h"
#include "CSEnumPropertyGenerator.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSEnumPropertyGenerator : public UCSPropertyGenerator
{
	GENERATED_BODY()

protected:
	// Begin UCSPropertyGenerator interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::Enum; }
	virtual FFieldClass* GetPropertyClass() override { return FEnumProperty::StaticClass(); }
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	virtual TSharedPtr<FCSUnrealType> CreateTypeMetaData(ECSPropertyType PropertyType) override;
#if WITH_EDITOR
	virtual FEdGraphPinType GetPinType(ECSPropertyType PropertyType, const FCSPropertyMetaData& MetaData, UBlueprint* Outer) const override;
#endif
	// End UCSPropertyGenerator interface
};

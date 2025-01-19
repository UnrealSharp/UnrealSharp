#pragma once

#include "CoreMinimal.h"
#include "CSDelegateBasePropertyGenerator.h"
#include "CSPropertyGenerator.h"
#include "CSMulticastDelegatePropertyGenerator.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSMulticastDelegatePropertyGenerator : public UCSDelegateBasePropertyGenerator
{
	GENERATED_BODY()

protected:

	// Begin UCSPropertyGenerator interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::MulticastInlineDelegate; }
	virtual FFieldClass* GetPropertyClass() override { return FMulticastInlineDelegateProperty::StaticClass(); }
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	virtual TSharedPtr<FCSUnrealType> CreateTypeMetaData(ECSPropertyType PropertyType) override;
#if WITH_EDITOR
	virtual FEdGraphPinType GetPinType(ECSPropertyType PropertyType, const FCSPropertyMetaData& MetaData, UBlueprint* Outer) const override;
#endif
	// End UCSPropertyGenerator interface
	
};

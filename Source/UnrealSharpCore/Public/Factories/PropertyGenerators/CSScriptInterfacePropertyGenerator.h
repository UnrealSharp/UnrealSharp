#pragma once

#include "CoreMinimal.h"
#include "CSPropertyGenerator.h"
#include "CSScriptInterfacePropertyGenerator.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSScriptInterfacePropertyGenerator : public UCSPropertyGenerator
{
	GENERATED_BODY()
protected:
	// Begin UCSPropertyGenerator interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::ScriptInterface; }
	virtual FFieldClass* GetPropertyClass() override { return FInterfaceProperty::StaticClass(); }
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	virtual TSharedPtr<FCSUnrealType> CreateTypeMetaData(ECSPropertyType PropertyType) override;
	// End UCSPropertyGenerator interface
};

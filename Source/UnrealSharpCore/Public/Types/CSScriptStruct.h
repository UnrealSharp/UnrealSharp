#pragma once

#include "StructUtils/UserDefinedStruct.h"
#include "CSManagedTypeInterface.h"
#include "CSScriptStruct.generated.h"

UCLASS()
class UCSScriptStruct : public UUserDefinedStruct, public ICSManagedTypeInterface
{
	GENERATED_BODY()
public:
	
	// UObject interface
	virtual bool IsFullNameStableForNetworking() const override { return true; }
	virtual bool IsNameStableForNetworking() const override { return true; }
	// End of UObject interface
	
	void Initialize();
	
private:
	void InitializeStructDefaults();
	TUniquePtr<uint8[]> StructDefaults;
};

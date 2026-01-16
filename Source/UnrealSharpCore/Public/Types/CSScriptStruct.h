#pragma once

#if ENGINE_MINOR_VERSION >= 5
#include "StructUtils/UserDefinedStruct.h"
#else
#include "Engine/UserDefinedStruct.h"
#endif
#include "CSManagedTypeInterface.h"
#include "CSScriptStruct.generated.h"

UCLASS(MinimalAPI)
class UCSScriptStruct : public UUserDefinedStruct, public ICSManagedTypeInterface
{
	GENERATED_BODY()
public:
	
	void Initialize();
	
private:
	void InitializeStructDefaults();
	TUniquePtr<uint8[]> StructDefaults;
};

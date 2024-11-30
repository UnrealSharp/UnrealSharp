#pragma once

#if ENGINE_MINOR_VERSION >= 5
#include "StructUtils/UserDefinedStruct.h"
#else
#include "Engine/UserDefinedStruct.h"
#endif

#include "CSScriptStruct.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSScriptStruct : public UUserDefinedStruct
{
	GENERATED_BODY()

public:

	void RecreateDefaults()
	{
		DefaultStructInstance.Recreate(this);
	}
};

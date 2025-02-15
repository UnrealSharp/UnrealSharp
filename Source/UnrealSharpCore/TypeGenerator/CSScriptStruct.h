#pragma once

#if ENGINE_MINOR_VERSION >= 5
#include "StructUtils/UserDefinedStruct.h"
#else
#include "Engine/UserDefinedStruct.h"
#endif

#include "CSScriptStruct.generated.h"

struct FCSharpStructInfo;

UCLASS()
class UNREALSHARPCORE_API UCSScriptStruct : public UUserDefinedStruct
{
	GENERATED_BODY()

public:

	void RecreateDefaults()
	{
		DefaultStructInstance.Recreate(this);
	}

	void SetStructInfo(const TSharedPtr<FCSharpStructInfo>& InStructInfo);
	TSharedPtr<FCSharpStructInfo> GetStructInfo() const { return StructInfo; }

private:
	TSharedPtr<FCSharpStructInfo> StructInfo;
};

#pragma once

#if ENGINE_MINOR_VERSION >= 5
#include "StructUtils/UserDefinedStruct.h"
#else
#include "Engine/UserDefinedStruct.h"
#endif
#include "ManagedReferencesCollection.h"

#include "CSScriptStruct.generated.h"

struct FCSharpStructInfo;

UCLASS(MinimalAPI)
class UCSScriptStruct : public UUserDefinedStruct
{
	GENERATED_BODY()

public:

	UNREALSHARPCORE_API TSharedPtr<FCSharpStructInfo> GetStructInfo() const { return StructInfo; }

	void RecreateDefaults();
	void SetStructInfo(const TSharedPtr<FCSharpStructInfo>& InStructInfo);

#if WITH_EDITORONLY_DATA
	UPROPERTY(Transient)
	FCSManagedReferencesCollection ManagedReferences;
#endif

private:
	TSharedPtr<FCSharpStructInfo> StructInfo;
};

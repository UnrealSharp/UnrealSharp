#pragma once

#if ENGINE_MINOR_VERSION >= 5
#include "StructUtils/UserDefinedStruct.h"
#else
#include "Engine/UserDefinedStruct.h"
#endif
#include "ManagedReferencesCollection.h"
#include "Utils/CSMacros.h"

#include "CSScriptStruct.generated.h"

UCLASS(MinimalAPI)
class UCSScriptStruct : public UUserDefinedStruct
{
	GENERATED_BODY()
	DECLARE_CSHARP_TYPE_FUNCTIONS(FCSStructInfo);
public:
	
	void RecreateDefaults() { DefaultStructInstance.Recreate(this); }

#if WITH_EDITORONLY_DATA
	UPROPERTY(Transient)
	FCSManagedReferencesCollection ManagedReferences;
#endif
};

#pragma once

#include "ManagedReferencesCollection.generated.h"

// Collection of weak references to keep track of managed references for fast access during hot reload.
// To keep track of types that needs to be recompiled due to structural changes.
USTRUCT()
struct UNREALSHARPCORE_API FCSManagedReferencesCollection
{
	GENERATED_BODY()
public:
	void AddReference(UStruct* Struct);
	void RemoveReference(UStruct* Struct);
	void ForEachManagedReference(const TFunction<void(UStruct*)>& Func);
private:
	UPROPERTY(Transient)
	TArray<TWeakObjectPtr<UStruct>> ManagedWeakReferences;
};

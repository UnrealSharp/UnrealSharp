#pragma once

// Collection of weak references to keep track of managed references for fast access during hot reload.
// To keep track of types that needs to be recompiled due to structural changes.
struct UNREALSHARPCORE_API FCSManagedReferencesCollection
{
	void AddReference(UStruct* Struct);
	void RemoveReference(UStruct* Struct);
	void ForEachManagedReference(const TFunction<void(UStruct*)>& Func);
private:
	TArray<TWeakObjectPtr<UStruct>> ManagedWeakReferences;
};

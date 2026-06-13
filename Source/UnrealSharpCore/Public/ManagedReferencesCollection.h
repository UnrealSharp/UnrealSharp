#pragma once

// Collection of weak references to keep track of managed references for fast access during hot reload.
// To keep track of types that needs to be recompiled due to structural changes.
#if WITH_EDITOR
struct UNREALSHARPCORE_API FCSReferencesCollection
{
	void AddReference(UStruct* Struct);
	void RemoveReference(UStruct* Struct);
	const TArray<TWeakObjectPtr<UStruct>>& GetReferences() { return References; }
private:
	TArray<TWeakObjectPtr<UStruct>> References;
};
#endif

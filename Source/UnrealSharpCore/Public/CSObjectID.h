#pragma once

#include "CSObjectID.generated.h"

USTRUCT()
struct FCSObjectID
{
	GENERATED_BODY()
	
	FCSObjectID() : Index(-1) {}
	FCSObjectID(int32 InIndex) : Index(InIndex) {}
	FCSObjectID(const UObjectBase* Object)
	{
		ensureMsgf(Object, TEXT("Attempted to create FCSObjectID from a null object pointer."));
		Index = Object->GetUniqueID();
	}
	
	int32 Get() const { return Index; }
	
	UObject* GetUObject() const
	{
		FUObjectItem* UObjectItem = GUObjectArray.IndexToObject(Index);
		return UObjectItem ? static_cast<UObject*>(UObjectItem->GetObject()) : nullptr;
	}
	
	template<typename T>
	T* GetUObject() const { return Cast<T>(GetUObject()); }
	
	friend uint32 GetTypeHash(const FCSObjectID& ObjectID) { return ObjectID.Index; }
	bool operator==(const FCSObjectID& Other) const { return Index == Other.Index; }
	bool operator!=(const FCSObjectID& Other) const { return Index != Other.Index; }
	
	static FCSObjectID Invalid() { return FCSObjectID(); }

private:
	int32 Index;
};

#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_FFieldPath)
{
	void Assign(FFieldPath* Destination, int32 Index, const FFieldPath& Source)
	{
		Destination[Index] = Source;
	}

	bool IsValid(const TFieldPath<FField>& FieldPath)
	{
		return FieldPath != nullptr;
	}

	bool IsStale(const FFieldPath& FieldPath)
	{
		return FieldPath.IsStale();
	}

	void FieldPathToString(const FFieldPath& FieldPath, FString* OutString)
	{
		*OutString = FieldPath.ToString();
	}

	bool FieldPathsEqual(const FFieldPath& A, const FFieldPath& B)
	{
		return A == B;
	}

	int32 GetFieldPathHashCode(const FFieldPath& FieldPath)
	{
		return static_cast<int32>(GetTypeHash(FieldPath));
	}
	
	BIND_UNREALSHARP_FUNCTION(Assign)
	BIND_UNREALSHARP_FUNCTION(IsValid)
	BIND_UNREALSHARP_FUNCTION(IsStale)
	BIND_UNREALSHARP_FUNCTION(FieldPathToString)
	BIND_UNREALSHARP_FUNCTION(FieldPathsEqual)
	BIND_UNREALSHARP_FUNCTION(GetFieldPathHashCode)
}

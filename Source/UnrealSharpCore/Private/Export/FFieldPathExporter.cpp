#include "CSBindsManager.h"

DECLARE_UNREALSHARP_EXPORTER(FFieldPathExporter)
{
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
	
	EXPORT_UNREALSHARP_FUNCTION(IsValid)
	EXPORT_UNREALSHARP_FUNCTION(IsStale)
	EXPORT_UNREALSHARP_FUNCTION(FieldPathToString)
	EXPORT_UNREALSHARP_FUNCTION(FieldPathsEqual)
	EXPORT_UNREALSHARP_FUNCTION(GetFieldPathHashCode)
}

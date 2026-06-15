
#include "CSBindsManager.h"
#include "UObject/PropertyOptional.h"

DECLARE_UNREALSHARP_EXPORTER(FOptionalPropertyExporter)
{
	bool IsSet(const FOptionalProperty* OptionalProperty, const void* ScriptValue)
	{
		return OptionalProperty->IsSet(ScriptValue);
	}

	void* MarkSetAndGetInitializedValuePointerToReplace(const FOptionalProperty* OptionalProperty, void* Data)
	{
		return OptionalProperty->MarkSetAndGetInitializedValuePointerToReplace(Data);
	}

	void MarkUnset(const FOptionalProperty* OptionalProperty, void* Data)
	{
		return OptionalProperty->MarkUnset(Data);
	}

	const void* GetValuePointerForRead(const FOptionalProperty* OptionalProperty, const void* Data)
	{
		return OptionalProperty->GetValuePointerForRead(Data);
	}

	void* GetValuePointerForReplace(const FOptionalProperty* OptionalProperty, void* Data)
	{
		return OptionalProperty->GetValuePointerForReplace(Data);
	}

	const void* GetValuePointerForReadIfSet(const FOptionalProperty* OptionalProperty, const void* Data)
	{
		return OptionalProperty->GetValuePointerForReadIfSet(Data);
	}

	void* GetValuePointerForReplaceIfSet(const FOptionalProperty* OptionalProperty, void* Data)
	{
		return OptionalProperty->GetValuePointerForReplaceIfSet(Data);
	}

	void* GetValuePointerForReadOrReplace(const FOptionalProperty* OptionalProperty, void* Data)
	{
		return OptionalProperty->GetValuePointerForReadOrReplace(Data);
	}

	void* GetValuePointerForReadOrReplaceIfSet(const FOptionalProperty* OptionalProperty, void* Data)
	{
		return OptionalProperty->GetValuePointerForReadOrReplaceIfSet(Data);
	}

	int32 CalcSize(const FOptionalProperty* OptionalProperty)
	{
		// Do we really need this? StaticLink should do this.
		return OptionalProperty->CalcSize();
	}

	void DestructInstance(const FOptionalProperty* OptionalProperty, void* Data)
	{
		OptionalProperty->DestroyValueInternal(Data);
	}
	
	EXPORT_UNREALSHARP_FUNCTION(IsSet)
	EXPORT_UNREALSHARP_FUNCTION(MarkSetAndGetInitializedValuePointerToReplace)
	EXPORT_UNREALSHARP_FUNCTION(MarkUnset)
	EXPORT_UNREALSHARP_FUNCTION(GetValuePointerForRead)
	EXPORT_UNREALSHARP_FUNCTION(GetValuePointerForReplace)
	EXPORT_UNREALSHARP_FUNCTION(GetValuePointerForReadIfSet)
	EXPORT_UNREALSHARP_FUNCTION(GetValuePointerForReplaceIfSet)
	EXPORT_UNREALSHARP_FUNCTION(GetValuePointerForReadOrReplace)
	EXPORT_UNREALSHARP_FUNCTION(GetValuePointerForReadOrReplaceIfSet)
	EXPORT_UNREALSHARP_FUNCTION(CalcSize)
	EXPORT_UNREALSHARP_FUNCTION(DestructInstance)
}

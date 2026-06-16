#include "CSBindsRegistry.h"
#include "UObject/PropertyOptional.h"

DECLARE_UNREALSHARP_BINDER(Bind_FOptionalProperty)
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
	
	BIND_UNREALSHARP_FUNCTION(IsSet)
	BIND_UNREALSHARP_FUNCTION(MarkSetAndGetInitializedValuePointerToReplace)
	BIND_UNREALSHARP_FUNCTION(MarkUnset)
	BIND_UNREALSHARP_FUNCTION(GetValuePointerForRead)
	BIND_UNREALSHARP_FUNCTION(GetValuePointerForReplace)
	BIND_UNREALSHARP_FUNCTION(GetValuePointerForReadIfSet)
	BIND_UNREALSHARP_FUNCTION(GetValuePointerForReplaceIfSet)
	BIND_UNREALSHARP_FUNCTION(GetValuePointerForReadOrReplace)
	BIND_UNREALSHARP_FUNCTION(GetValuePointerForReadOrReplaceIfSet)
	BIND_UNREALSHARP_FUNCTION(CalcSize)
	BIND_UNREALSHARP_FUNCTION(DestructInstance)
}

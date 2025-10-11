#include "FOptionalPropertyExporter.h"
#include "UObject/PropertyOptional.h"

bool UFOptionalPropertyExporter::IsSet(const FOptionalProperty* OptionalProperty, const void* ScriptValue)
{
	return OptionalProperty->IsSet(ScriptValue);
}

void* UFOptionalPropertyExporter::MarkSetAndGetInitializedValuePointerToReplace(const FOptionalProperty* OptionalProperty, void* Data)
{
	return OptionalProperty->MarkSetAndGetInitializedValuePointerToReplace(Data);
}

void UFOptionalPropertyExporter::MarkUnset(const FOptionalProperty* OptionalProperty, void* Data)
{
	return OptionalProperty->MarkUnset(Data);
}

const void* UFOptionalPropertyExporter::GetValuePointerForRead(const FOptionalProperty* OptionalProperty, const void* Data)
{
	return OptionalProperty->GetValuePointerForRead(Data);
}

void* UFOptionalPropertyExporter::GetValuePointerForReplace(const FOptionalProperty* OptionalProperty, void* Data)
{
	return OptionalProperty->GetValuePointerForReplace(Data);
}

const void* UFOptionalPropertyExporter::GetValuePointerForReadIfSet(const FOptionalProperty* OptionalProperty, const void* Data)
{
	return OptionalProperty->GetValuePointerForReadIfSet(Data);
}

void* UFOptionalPropertyExporter::GetValuePointerForReplaceIfSet(const FOptionalProperty* OptionalProperty, void* Data)
{
	return OptionalProperty->GetValuePointerForReplaceIfSet(Data);
}

void* UFOptionalPropertyExporter::GetValuePointerForReadOrReplace(const FOptionalProperty* OptionalProperty, void* Data)
{
	return OptionalProperty->GetValuePointerForReadOrReplace(Data);
}

void* UFOptionalPropertyExporter::GetValuePointerForReadOrReplaceIfSet(const FOptionalProperty* OptionalProperty, void* Data)
{
	return OptionalProperty->GetValuePointerForReadOrReplaceIfSet(Data);
}

int32 UFOptionalPropertyExporter::CalcSize(const FOptionalProperty* OptionalProperty)
{
#if ENGINE_MINOR_VERSION >= 5
	// Do we really need this? StaticLink should do this.
	return OptionalProperty->CalcSize();
#else
	return OptionalProperty->GetSize();
#endif
}

void UFOptionalPropertyExporter::DestructInstance(const FOptionalProperty* OptionalProperty, void* Data)
{
    OptionalProperty->DestroyValueInternal(Data);
}

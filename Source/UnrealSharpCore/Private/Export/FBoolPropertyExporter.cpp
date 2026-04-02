#include "Export/FBoolPropertyExporter.h"

bool UFBoolPropertyExporter::GetBitfieldValueFromProperty(uint8* NativeBuffer, FProperty* Property, int32 Offset)
{
	// NativeBuffer won't necessarily correspond to a UObject.  It might be the beginning of a native struct, for example.
	check(NativeBuffer);
	uint8* OffsetPointer = NativeBuffer + Offset;
	check(OffsetPointer == Property->ContainerPtrToValuePtr<uint8>(NativeBuffer));
	FBoolProperty* BoolProperty = CastFieldChecked<FBoolProperty>(Property);
	return BoolProperty->GetPropertyValue(OffsetPointer);
}

void UFBoolPropertyExporter::SetBitfieldValueForProperty(uint8* NativeObject, FProperty* Property, int32 Offset, bool Value)
{
	// NativeBuffer won't necessarily correspond to a UObject.  It might be the beginning of a native struct, for example.
	check(NativeObject);
	uint8* OffsetPointer = NativeObject + Offset;
	check(OffsetPointer == Property->ContainerPtrToValuePtr<uint8>(NativeObject));
	const FBoolProperty* BoolProperty = CastFieldChecked<FBoolProperty>(Property);
	BoolProperty->SetPropertyValue(OffsetPointer, Value);
}

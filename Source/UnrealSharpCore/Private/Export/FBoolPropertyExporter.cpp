#include "CSBindsManager.h"

DECLARE_UNREALSHARP_EXPORTER(FBoolPropertyExporter)
{
	bool GetBitfieldValueFromProperty(uint8* NativeBuffer, FProperty* Property, int32 Offset)
	{
		check(NativeBuffer);
		uint8* OffsetPointer = NativeBuffer + Offset;
		check(OffsetPointer == Property->ContainerPtrToValuePtr<uint8>(NativeBuffer));
		FBoolProperty* BoolProperty = CastFieldChecked<FBoolProperty>(Property);
		return BoolProperty->GetPropertyValue(OffsetPointer);
	}

	void SetBitfieldValueForProperty(uint8* NativeObject, FProperty* Property, int32 Offset, bool Value)
	{
		check(NativeObject);
		uint8* OffsetPointer = NativeObject + Offset;
		check(OffsetPointer == Property->ContainerPtrToValuePtr<uint8>(NativeObject));
		const FBoolProperty* BoolProperty = CastFieldChecked<FBoolProperty>(Property);
		BoolProperty->SetPropertyValue(OffsetPointer, Value);
	}
	
	EXPORT_UNREALSHARP_FUNCTION(GetBitfieldValueFromProperty)
	EXPORT_UNREALSHARP_FUNCTION(SetBitfieldValueForProperty)
}

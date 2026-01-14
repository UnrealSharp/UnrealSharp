#include "Types/CSScriptStruct.h"

void UCSScriptStruct::Initialize()
{
#if WITH_EDITOR
	PrimaryStruct = this;
#endif
	
	InitializeStructDefaults();
	DefaultStructInstance = FUserStructOnScopeIgnoreDefaults(this, StructDefaults.Get());
	UpdateStructFlags();
}

void UCSScriptStruct::InitializeStruct(void* Dest, int32 ArrayDim) const
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSScriptStruct::InitializeStruct);
	
	int32 Size = GetStructureSize();
	uint8* DefaultData = StructDefaults.Get();
	
	if (Size <= 0 || !DefaultData)
	{
		return;
	}
	
	for (FProperty* Property = PropertyLink; Property; Property = Property->PropertyLinkNext)
	{
		auto CalculateOffset = [](FProperty* Property, int32 Index, int32 Size, void* Data) -> uint8*
		{
			return static_cast<uint8*>(Data) + Index * Size + Property->GetOffset_ForInternal();
		};
		
		for (int32 Index = 0; Index < ArrayDim; Index++)
		{
			uint8* DestPtr = CalculateOffset(Property, Index, Size, Dest);
			uint8* SrcPtr = CalculateOffset(Property, Index, Size, DefaultData);
			Property->CopyCompleteValue(DestPtr, SrcPtr);
		}
	}
}

void UCSScriptStruct::InitializeStructDefaults()
{
	int32 Size = GetStructureSize();
	
	if (Size <= 0)
	{
		return;
	}
	
	StructDefaults = MakeUnique<uint8[]>(Size);
	FCSManagedCallbacks::ManagedCallbacks.InitializeStructure(GetTypeGCHandle()->GetHandle(), StructDefaults.Get());
}

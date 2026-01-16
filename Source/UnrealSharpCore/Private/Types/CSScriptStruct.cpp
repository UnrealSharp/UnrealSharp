#include "Types/CSScriptStruct.h"

void UCSScriptStruct::Initialize()
{
#if WITH_EDITOR
	PrimaryStruct = this;
#endif
	
	InitializeStructDefaults();
	DefaultStructInstance = FUserStructOnScopeIgnoreDefaults(this, StructDefaults.Get());
	DefaultStructInstance.SetPackage(GetOutermost());
	UpdateStructFlags();
}

void UCSScriptStruct::InitializeStruct(void* Dest, int32 ArrayDim) const
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSScriptStruct::InitializeStruct);
	
	if ((StructFlags & STRUCT_IsPlainOldData) == 0)
	{
		InitializeStructIgnoreDefaults(Dest, ArrayDim);
	}
	
	int32 Size = GetStructureSize();
	
	for (int32 ArrayIndex = 0; ArrayIndex < ArrayDim; ArrayIndex++)
	{
		void* DestStruct = static_cast<uint8*>(Dest) + (Size * ArrayIndex);
		CopyScriptStruct(DestStruct, StructDefaults.Get());
	}
}

void UCSScriptStruct::InitializeStructDefaults()
{
	int32 Size = FMath::Max(GetStructureSize(), 1);
	StructDefaults = MakeUnique<uint8[]>(Size);
	
	InitializeStructIgnoreDefaults(StructDefaults.Get());
	
	FCSManagedCallbacks::ManagedCallbacks.InitializeStructure(GetTypeGCHandle()->GetHandle(), StructDefaults.Get());
}

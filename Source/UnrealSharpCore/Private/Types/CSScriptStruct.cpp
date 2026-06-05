#include "Types/CSScriptStruct.h"

void UCSScriptStruct::Initialize()
{
#if WITH_EDITOR
	PrimaryStruct = this;
#endif
	
	InitializeStructDefaults();
	UpdateStructFlags();
}

void UCSScriptStruct::InitializeStructDefaults()
{
	int32 Size = FMath::Max(GetStructureSize(), 1);
	StructDefaults = MakeUnique<uint8[]>(Size);
	
	InitializeStructIgnoreDefaults(StructDefaults.Get());
	
	GetManagedCallbacks().InitializeStructure(GetTypeGCHandle()->GetHandle(), StructDefaults.Get());
	
	DefaultStructInstance = FUserStructOnScopeIgnoreDefaults(this, StructDefaults.Get());
	DefaultStructInstance.SetPackage(GetOutermost());
}
